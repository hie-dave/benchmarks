using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Services.Metrics;
using Dave.Benchmarks.Core.Services.Spatial;
using LpjGuess.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.Core.Services.Evaluation;

public class EvaluationEngine : IEvaluationEngine
{
    private readonly BenchmarksDbContext db;
    private readonly ILogger<EvaluationEngine> logger;
    private record struct Results(bool Passed, IEnumerable<EvaluationResult> EvaluationResults);

    public EvaluationEngine(BenchmarksDbContext db, ILogger<EvaluationEngine> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int evaluationRunId, CancellationToken cancellationToken = default)
    {
        EvaluationRun? run = await db.EvaluationRuns
            .Include(r => r.Datasets)
            .FirstOrDefaultAsync(r => r.Id == evaluationRunId, cancellationToken);

        if (run == null)
            throw new InvalidOperationException($"Evaluation run {evaluationRunId} not found");

        run.Status = EvaluationRunStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        run.ErrorMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            bool passed = true;
            foreach (EvaluationRunDataset runDataset in run.Datasets)
            {
                runDataset.Status = EvaluationRunStatus.Running;
                runDataset.StartedAt = DateTime.UtcNow;

                PredictionDataset candidate = await db.Datasets
                    .OfType<PredictionDataset>()
                    .Include(d => d.Variables).ThenInclude(v => v.Layers)
                    .FirstOrDefaultAsync(d => d.Id == runDataset.CandidateDatasetId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Candidate prediction dataset {runDataset.CandidateDatasetId} not found");

                PredictionDataset? baseline = await ResolveBaselineDataset(runDataset, candidate, cancellationToken);
                if (baseline != null)
                    runDataset.BaselineDatasetId = baseline.Id;

                Results results = await BuildObservationResults(runDataset, candidate, baseline, cancellationToken);
                db.EvaluationResults.AddRange(results.EvaluationResults);
                runDataset.Passed = results.Passed;
                runDataset.Status = EvaluationRunStatus.Succeeded;
                runDataset.CompletedAt = DateTime.UtcNow;
                passed &= results.Passed;
                await db.SaveChangesAsync(cancellationToken);
            }

            run.Passed = passed;
            run.Status = EvaluationRunStatus.Succeeded;
            run.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Evaluation run {RunId} failed", evaluationRunId);

            run.Status = EvaluationRunStatus.Failed;
            run.Passed = false;
            run.CompletedAt = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
            foreach (EvaluationRunDataset dataset in run.Datasets.Where(d =>
                         d.Status is EvaluationRunStatus.Pending or EvaluationRunStatus.Running))
            {
                dataset.Status = EvaluationRunStatus.Failed;
                dataset.Passed = false;
                dataset.CompletedAt = DateTime.UtcNow;
                dataset.ErrorMessage = ex.Message;
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<PredictionDataset?> ResolveBaselineDataset(
        EvaluationRunDataset runDataset,
        PredictionDataset candidate,
        CancellationToken cancellationToken)
    {
        PredictionDataset? baseline = null;
        if (runDataset.BaselineDatasetId.HasValue)
        {
            return await db.Datasets
                .OfType<PredictionDataset>()
                .Include(d => d.Variables)
                    .ThenInclude(v => v.Layers)
                .FirstOrDefaultAsync(d => d.Id == runDataset.BaselineDatasetId.Value, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Baseline prediction dataset {runDataset.BaselineDatasetId.Value} not found");
        }

        PredictionBaselineRegistryEntry? baselineEntry = await db.PredictionBaselineRegistryEntries
            .OrderByDescending(e => e.AcceptedAt)
            .FirstOrDefaultAsync(
                e => e.SimulationId == candidate.SimulationId &&
                        e.BaselineChannel == candidate.BaselineChannel,
                cancellationToken);

        if (baselineEntry == null)
            // No accepted baseline for this simulation/channel.
            return null;

        baseline = await db.Datasets
            .OfType<PredictionDataset>()
            .Include(d => d.Variables)
                .ThenInclude(v => v.Layers)
            .FirstOrDefaultAsync(d => d.Id == baselineEntry.PredictionDatasetId, cancellationToken);

        return baseline;
    }

    private async Task<Results> BuildObservationResults(
        EvaluationRunDataset runDataset,
        PredictionDataset candidate,
        PredictionDataset? baseline,
        CancellationToken cancellationToken)
    {
        List<EvaluationResult> results = [];

        List<ObservationDataset> activeObservations = await db.Datasets
            .OfType<ObservationDataset>()
            .Where(d => d.Group != null && d.Group.IsComplete && d.Group.IsActive)
            .Include(d => d.Variables)
                .ThenInclude(v => v.Layers)
            .ToListAsync(cancellationToken);

        bool pass = true;
        EvaluationRun? baselineRun = null;
        if (baseline != null)
        {
            baselineRun = db.EvaluationRuns
                .Include(r => r.Datasets).ThenInclude(d => d.Results).ThenInclude(r => r.Metrics)
                .OrderByDescending(r => r.StartedAt)
                .FirstOrDefault(r => r.Datasets.Any(d => d.CandidateDatasetId == baseline.Id));
        }

        foreach (ObservationDataset observationDataset in activeObservations)
        {
            if (!ObservationDatasetApplies(observationDataset, candidate))
                continue;

            foreach (Variable candidateVar in candidate.Variables)
            {
                if (candidateVar.Level != AggregationLevel.Gridcell)
                    continue;

                // Could an observation dataset have multiple variables which
                // match? In practice, probably not.
                Variable? observationVar = observationDataset.Variables.FirstOrDefault(v =>
                    VariablesComparable(candidateVar, v));

                if (observationVar == null)
                    continue;

                Variable? baselineVar = baseline?.Variables.FirstOrDefault(v =>
                    VariablesComparable(candidateVar, v));

                foreach (VariableLayer candidateLayer in candidateVar.Layers)
                {
                    VariableLayer? observationLayer = observationVar.Layers
                        .FirstOrDefault(l => LayersComparable(candidateLayer, l));
                    if (observationLayer == null)
                        continue;

                    VariableLayer? baselineLayer = baselineVar?.Layers
                        .FirstOrDefault(l => LayersComparable(candidateLayer, l));

                    EvaluationResult? baselineResult = baseline == null ? null : baselineRun?.Datasets
                        .FirstOrDefault(d => d.CandidateDatasetId == baseline.Id)?.Results
                        .FirstOrDefault(r => r.CandidateVariableId == baselineVar?.Id &&
                                    r.CandidateLayerId == baselineLayer?.Id &&
                                    r.ObservationVariableId == observationVar.Id &&
                                    r.ObservationLayerId == observationLayer.Id);

                    Dictionary<PointKey, PointValue> candidatePoints = await LoadSeriesWithCoordinates(
                        candidateVar.Level, candidateVar.Id, candidateLayer.Id, cancellationToken);
                    Dictionary<PointKey, PointValue> observationPoints = await LoadSeriesWithCoordinates(
                        observationVar.Level, observationVar.Id, observationLayer.Id, cancellationToken);

                    List<MetricSeries> pairs = MatchSeries(
                        observationDataset.MatchingStrategy,
                        observationDataset.MaxDistance,
                        candidatePoints,
                        observationPoints);

                    if (pairs.Count == 0)
                        continue;

                    EvaluationResult result = new()
                    {
                        EvaluationRunDataset = runDataset,
                        CandidateVariableId = candidateVar.Id,
                        CandidateLayerId = candidateLayer.Id,
                        BaselineVariableId = baselineVar?.Id,
                        BaselineLayerId = baselineLayer?.Id,
                        ObservationVariableId = observationVar.Id,
                        ObservationLayerId = observationLayer.Id
                    };

                    foreach (IMetric metric in BuiltInMetrics.All)
                    {
                        double? metricValue = metric.Compute(pairs);
                        if (!metricValue.HasValue)
                            continue;

                        result.Metrics.Add(new EvaluationMetric
                        {
                            MetricType = metric.Type,
                            Value = metricValue.Value
                        });

                        EvaluationMetric? baselineMetric = baselineResult?.Metrics.FirstOrDefault(m => m.MetricType == metric.Type);
                        if (baselineMetric != null)
                        {
                            bool improvement = metric.IsImprovement(baselineMetric.Value, metricValue.Value);
                            if (!improvement)
                                pass = false;
                        }
                    }
                    results.Add(result);
                }
            }
        }
        return new Results(pass, results);
    }

    private static bool VariablesComparable(Variable left, Variable right)
    {
        if (left.Level != AggregationLevel.Gridcell || right.Level != AggregationLevel.Gridcell)
            return false;
        if (!string.IsNullOrWhiteSpace(left.ComparisonOutput) &&
            !string.IsNullOrWhiteSpace(right.ComparisonOutput))
            return left.ComparisonOutput == right.ComparisonOutput;
        return left.Name == right.Name && left.Units == right.Units;
    }

    private static bool LayersComparable(VariableLayer left, VariableLayer right)
    {
        if (!string.IsNullOrWhiteSpace(left.ComparisonLayer) &&
            !string.IsNullOrWhiteSpace(right.ComparisonLayer))
            return left.ComparisonLayer == right.ComparisonLayer;
        return left.Name.Equals(right.Name, StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool ObservationDatasetApplies(ObservationDataset observation, PredictionDataset candidate)
    {
        return observation.MatchingStrategy switch
        {
            MatchingStrategy.ByName => !string.IsNullOrWhiteSpace(observation.SimulationId) &&
                                       observation.SimulationId == candidate.SimulationId,
            _ => true
        };
    }

    private List<MetricSeries> MatchSeries(
        MatchingStrategy strategy,
        int? maxDistanceKm,
        IReadOnlyDictionary<PointKey, PointValue> candidate,
        IReadOnlyDictionary<PointKey, PointValue> observation)
    {
        return strategy switch
        {
            MatchingStrategy.ByName => MatchByName(candidate, observation),
            MatchingStrategy.ExactMatch => MatchExact(candidate, observation),
            MatchingStrategy.Nearest => MatchNearest(candidate, observation, maxDistanceKm ?? 0),
            _ => throw new InvalidOperationException($"Unsupported matching strategy {strategy}")
        };
    }

    private static List<MetricSeries> MatchByName(
        IReadOnlyDictionary<PointKey, PointValue> candidate,
        IReadOnlyDictionary<PointKey, PointValue> observation)
    {
        var candidateByTime = candidate.Values
            .GroupBy(v => v.Timestamp)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Latitude).ThenBy(p => p.Longitude).ToList());
        var observationByTime = observation.Values
            .GroupBy(v => v.Timestamp)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Latitude).ThenBy(p => p.Longitude).ToList());

        List<MetricSeries> pairs = [];
        foreach ((DateTime timestamp, List<PointValue> obsPoints) in observationByTime)
        {
            if (!candidateByTime.TryGetValue(timestamp, out List<PointValue>? candPoints))
                continue;

            int n = Math.Min(obsPoints.Count, candPoints.Count);
            for (int i = 0; i < n; i++)
                pairs.Add(new MetricSeries(obsPoints[i].Value, candPoints[i].Value));
        }

        return pairs;
    }

    private static List<MetricSeries> MatchExact(
        IReadOnlyDictionary<PointKey, PointValue> candidate,
        IReadOnlyDictionary<PointKey, PointValue> observation)
    {
        List<MetricSeries> pairs = [];
        foreach ((PointKey key, PointValue obs) in observation)
        {
            if (candidate.TryGetValue(key, out PointValue cand))
                pairs.Add(new MetricSeries(obs.Value, cand.Value));
        }

        return pairs;
    }

    private static List<MetricSeries> MatchNearest(
        IReadOnlyDictionary<PointKey, PointValue> candidate,
        IReadOnlyDictionary<PointKey, PointValue> observation,
        int maxDistanceKm)
    {
        if (maxDistanceKm <= 0)
            return [];

        List<MetricSeries> pairs = [];
        foreach (PointValue obs in observation.Values)
        {
            PointValue? nearest = candidate.Values
                .Where(c => c.Timestamp == obs.Timestamp &&
                            c.StandId == obs.StandId &&
                            c.PatchId == obs.PatchId &&
                            c.IndividualNumber == obs.IndividualNumber)
                            .Where(c => obs.Latitude.HasValue && obs.Longitude.HasValue && c.Latitude.HasValue && c.Longitude.HasValue)
                            .Select(c => new { Point = c, Distance = GeoDistance.HaversineKm(obs.Latitude!.Value, obs.Longitude!.Value, c.Latitude!.Value, c.Longitude!.Value) })
                .Where(x => x.Distance <= maxDistanceKm)
                .OrderBy(x => x.Distance)
                .Select(x => (PointValue?)x.Point)
                .FirstOrDefault();

            if (nearest.HasValue)
                pairs.Add(new MetricSeries(obs.Value, nearest.Value.Value));
        }

        return pairs;
    }

    private async Task<Dictionary<PointKey, PointValue>> LoadSeriesWithCoordinates(
        AggregationLevel level,
        int variableId,
        int layerId,
        CancellationToken cancellationToken)
    {
        switch (level)
        {
            case AggregationLevel.Gridcell:
                {
                    var points = await db.GridcellData
                        .Where(d => d.VariableId == variableId && d.LayerId == layerId)
                        .Select(d => new PointValue(d.Timestamp, d.Latitude, d.Longitude, d.Value, null, null, null))
                        .ToListAsync(cancellationToken);
                    return points.ToDictionary(
                        p => new PointKey(p.Timestamp, p.Latitude, p.Longitude, null, null, null),
                        p => p);
                }
            case AggregationLevel.Stand:
                {
                    var points = await db.StandData
                        .Where(d => d.VariableId == variableId && d.LayerId == layerId)
                        .Select(d => new PointValue(d.Timestamp, d.Latitude, d.Longitude, d.Value, d.StandId, null, null))
                        .ToListAsync(cancellationToken);
                    return points.ToDictionary(
                        p => new PointKey(p.Timestamp, p.Latitude, p.Longitude, p.StandId, null, null),
                        p => p);
                }
            case AggregationLevel.Patch:
                {
                    var points = await db.PatchData
                        .Where(d => d.VariableId == variableId && d.LayerId == layerId)
                        .Select(d => new PointValue(d.Timestamp, d.Latitude, d.Longitude, d.Value, d.StandId, d.PatchId, null))
                        .ToListAsync(cancellationToken);
                    return points.ToDictionary(
                        p => new PointKey(p.Timestamp, p.Latitude, p.Longitude, p.StandId, p.PatchId, null),
                        p => p);
                }
            case AggregationLevel.Individual:
                {
                    var points = await db.IndividualData
                        .Where(d => d.VariableId == variableId && d.LayerId == layerId)
                        .Include(d => d.Individual)
                        .Select(d => new PointValue(
                            d.Timestamp,
                            d.Latitude,
                            d.Longitude,
                            d.Value,
                            d.StandId,
                            d.PatchId,
                            d.Individual.Number))
                        .ToListAsync(cancellationToken);
                    return points.ToDictionary(
                        p => new PointKey(p.Timestamp, p.Latitude, p.Longitude, p.StandId, p.PatchId, p.IndividualNumber),
                        p => p);
                }
            default:
                throw new InvalidOperationException($"Unsupported aggregation level {level}");
        }
    }

    private readonly record struct PointKey(
        DateTime Timestamp,
        double? Latitude,
        double? Longitude,
        int? StandId,
        int? PatchId,
        int? IndividualNumber);

    private readonly record struct PointValue(
        DateTime Timestamp,
        double? Latitude,
        double? Longitude,
        double Value,
        int? StandId,
        int? PatchId,
        int? IndividualNumber);
}
