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
            .FirstOrDefaultAsync(r => r.Id == evaluationRunId, cancellationToken);

        if (run == null)
            throw new InvalidOperationException($"Evaluation run {evaluationRunId} not found");

        run.Status = EvaluationRunStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        run.ErrorMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            PredictionDataset candidate = await db.Datasets
                .OfType<PredictionDataset>()
                .Include(d => d.Variables)
                    .ThenInclude(v => v.Layers)
                .FirstOrDefaultAsync(d => d.Id == run.CandidateDatasetId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Candidate prediction dataset {run.CandidateDatasetId} not found");

            PredictionDataset? baseline = await ResolveBaselineDataset(run, candidate, cancellationToken);

            if (baseline != null)
            {
                run.BaselineDatasetId = baseline.Id;
                await db.SaveChangesAsync(cancellationToken);
            }

            // Build observation-based evaluation result rows with metrics.
            Results results = await BuildObservationResults(run, candidate, baseline, cancellationToken);

            // Update DB.
            db.EvaluationResults.AddRange(results.EvaluationResults);
            run.Passed = results.Passed;
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
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<PredictionDataset?> ResolveBaselineDataset(
        EvaluationRun run,
        PredictionDataset candidate,
        CancellationToken cancellationToken)
    {
        PredictionDataset? baseline = null;
        if (run.BaselineDatasetId.HasValue)
        {
            return await db.Datasets
                .OfType<PredictionDataset>()
                .Include(d => d.Variables)
                    .ThenInclude(v => v.Layers)
                .FirstOrDefaultAsync(d => d.Id == run.BaselineDatasetId.Value, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Baseline prediction dataset {run.BaselineDatasetId.Value} not found");
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
        EvaluationRun run,
        PredictionDataset candidate,
        PredictionDataset? baseline,
        CancellationToken cancellationToken)
    {
        List<EvaluationResult> results = [];

        List<ObservationDataset> activeObservations = await db.Datasets
            .OfType<ObservationDataset>()
            .Where(d => d.Active)
            .Include(d => d.Variables)
                .ThenInclude(v => v.Layers)
            .ToListAsync(cancellationToken);

        bool pass = true;
        EvaluationRun? baselineRun = null;
        if (baseline != null)
        {
            baselineRun = db.EvaluationRuns
            .Include(r => r.Results)
            .ThenInclude(r => r.Metrics)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefault(r => r.CandidateDatasetId == baseline.Id);
        }

        foreach (ObservationDataset observationDataset in activeObservations)
        {
            if (!ObservationDatasetApplies(observationDataset, candidate))
                continue;

            foreach (Variable candidateVar in candidate.Variables)
            {
                // Could an observation dataset have multiple variables which
                // match? In practice, probably not.
                Variable? observationVar = observationDataset.Variables.FirstOrDefault(v =>
                    v.Name == candidateVar.Name &&
                    v.Level == candidateVar.Level &&
                    v.Units == candidateVar.Units);

                if (observationVar == null)
                    continue;

                Variable? baselineVar = baseline?.Variables.FirstOrDefault(v =>
                    v.Name == candidateVar.Name &&
                    // Baseline is a prediction dataset, so its description
                    // could and should match the candidate's as well.
                    // TODO: is this brittle wrt description? Should probably
                    // rethink this.
                    v.Description == candidateVar.Description &&
                    v.Level == candidateVar.Level &&
                    v.Units == candidateVar.Units);

                foreach (VariableLayer candidateLayer in candidateVar.Layers)
                {
                    VariableLayer? observationLayer = observationVar.Layers
                        .FirstOrDefault(l => l.Name.Equals(candidateLayer.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (observationLayer == null)
                        continue;

                    VariableLayer? baselineLayer = baselineVar?.Layers
                        .FirstOrDefault(l => l.Name.Equals(candidateLayer.Name, StringComparison.InvariantCultureIgnoreCase));

                    EvaluationResult? baselineResult = baselineRun?.Results
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
                        EvaluationRun = run,
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
                .Select(c => new { Point = c, Distance = GeoDistance.HaversineKm(obs.Latitude, obs.Longitude, c.Latitude, c.Longitude) })
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
        double Latitude,
        double Longitude,
        int? StandId,
        int? PatchId,
        int? IndividualNumber);

    private readonly record struct PointValue(
        DateTime Timestamp,
        double Latitude,
        double Longitude,
        double Value,
        int? StandId,
        int? PatchId,
        int? IndividualNumber);
}
