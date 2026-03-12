using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Services.Evaluation;
using Dave.Benchmarks.Tests.Helpers;
using LpjGuess.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class EvaluationEngineTests
{
    [Fact]
    public async Task ExecuteAsync_SuccessPath_UpdatesRunAndPersistsResultsAndMetrics()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "cand");
        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "base");
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, MatchingStrategy.ExactMatch, active: true);

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        (Variable bVar, VariableLayer bLayer) = EvaluationSeed.AddVariableLayer(db, baseline);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs);

        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, bVar, bLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -33, 151, 1.0);

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());

        await engine.ExecuteAsync(run.Id);

        EvaluationRun updated = db.EvaluationRuns.Single(r => r.Id == run.Id);
        Assert.Equal(EvaluationRunStatus.Succeeded, updated.Status);
        Assert.True(updated.Passed);
        Assert.NotNull(updated.CompletedAt);
        Assert.Null(updated.ErrorMessage);

        EvaluationResult result = db.EvaluationResults
            .Include(r => r.Metrics)
            .Single(r => r.EvaluationRunId == run.Id);
        Assert.Equal(cVar.Id, result.CandidateVariableId);
        Assert.Equal(cLayer.Id, result.CandidateLayerId);
        Assert.Equal(oVar.Id, result.ObservationVariableId);
        Assert.Equal(oLayer.Id, result.ObservationLayerId);
        Assert.Contains(result.Metrics, m => m.MetricType == "n" && Math.Abs(m.Value - 1.0) < 0.001);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_MarksRunFailed()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "cand");
        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "base");
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, MatchingStrategy.ExactMatch, active: true);
        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        (Variable bVar, VariableLayer bLayer) = EvaluationSeed.AddVariableLayer(db, baseline);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs);

        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, bVar, bLayer, t, -33, 151, 1.0);
        // Duplicate key in observation series causes ToDictionary() failure in engine.
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -33, 151, 2.0);

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());

        await engine.ExecuteAsync(run.Id);

        EvaluationRun updated = db.EvaluationRuns.Single(r => r.Id == run.Id);
        Assert.Equal(EvaluationRunStatus.Failed, updated.Status);
        Assert.False(updated.Passed);
        Assert.NotNull(updated.CompletedAt);
        Assert.False(string.IsNullOrWhiteSpace(updated.ErrorMessage));
    }

    [Fact]
    public async Task ExecuteAsync_FallbackBaseline_UsesLatestAcceptedByDate()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "cand");
        PredictionDataset olderBaseline = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "old");
        PredictionDataset newerBaseline = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "new");

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, olderBaseline);
        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -33, 151, 1.0); // would pass if picked
        // Deliberately leave newer baseline without matching variable/layer.

        db.PredictionBaselineRegistryEntries.AddRange(
            new PredictionBaselineRegistryEntry
            {
                SimulationId = "sim",
                BaselineChannel = "main",
                PredictionDatasetId = olderBaseline.Id,
                AcceptedAt = DateTime.UtcNow.AddMinutes(-5),
                AcceptedBy = "ci"
            },
            new PredictionBaselineRegistryEntry
            {
                SimulationId = "sim",
                BaselineChannel = "main",
                PredictionDatasetId = newerBaseline.Id,
                AcceptedAt = DateTime.UtcNow,
                AcceptedBy = "ci"
            });
        db.SaveChanges();

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baselineDatasetId: null);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());

        await engine.ExecuteAsync(run.Id);

        EvaluationRun updated = db.EvaluationRuns.Single(r => r.Id == run.Id);
        Assert.Equal(newerBaseline.Id, updated.BaselineDatasetId);
        Assert.Equal(EvaluationRunStatus.Succeeded, updated.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitBaselineId_IsUsed()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "cand");
        PredictionDataset explicitBaseline = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "explicit");
        PredictionDataset registryBaseline = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "registry");

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        (Variable eVar, VariableLayer eLayer) = EvaluationSeed.AddVariableLayer(db, explicitBaseline);
        (Variable rVar, VariableLayer rLayer) = EvaluationSeed.AddVariableLayer(db, registryBaseline);
        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, eVar, eLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, rVar, rLayer, t, -33, 151, 2.0);

        db.PredictionBaselineRegistryEntries.Add(new PredictionBaselineRegistryEntry
        {
            SimulationId = "sim",
            BaselineChannel = "main",
            PredictionDatasetId = registryBaseline.Id,
            AcceptedAt = DateTime.UtcNow,
            AcceptedBy = "ci"
        });
        db.SaveChanges();

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, explicitBaseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());

        await engine.ExecuteAsync(run.Id);

        EvaluationRun updated = db.EvaluationRuns.Single(r => r.Id == run.Id);
        Assert.Equal(explicitBaseline.Id, updated.BaselineDatasetId);
        Assert.True(updated.Passed);
    }

    [Fact]
    public async Task ExecuteAsync_ByNameObservationRequiresMatchingSimulationId()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db, "sim-a", "main");
        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db, "sim-a", "main");
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(
            db,
            MatchingStrategy.ByName,
            simulationId: "different-sim");

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        (Variable bVar, VariableLayer bLayer) = EvaluationSeed.AddVariableLayer(db, baseline);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs);
        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, bVar, bLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -33, 151, 1.0);

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());

        await engine.ExecuteAsync(run.Id);

        Assert.Empty(db.EvaluationResults.Where(r => r.EvaluationRunId == run.Id));
    }

    [Fact]
    public async Task ExecuteAsync_ExactMatch_UsesKeyIntersection()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db);
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, MatchingStrategy.ExactMatch);

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        (Variable bVar, VariableLayer bLayer) = EvaluationSeed.AddVariableLayer(db, baseline);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs);
        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -34, 150, 1.5);
        EvaluationSeed.AddGridcellDatum(db, bVar, bLayer, t, -33, 151, 1.0);
        EvaluationSeed.AddGridcellDatum(db, bVar, bLayer, t, -34, 150, 1.5);
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -33, 151, 1.0); // one matching point

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());

        await engine.ExecuteAsync(run.Id);

        EvaluationResult result = db.EvaluationResults
            .Include(r => r.Metrics)
            .Single(r => r.EvaluationRunId == run.Id);
        Assert.Contains(result.Metrics, m => m.MetricType == "n" && Math.Abs(m.Value - 1.0) < 0.001);
    }

    [Fact]
    public async Task ExecuteAsync_NearestMatch_RespectsMaxDistance()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db);
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, MatchingStrategy.Nearest, maxDistance: 10);

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        (Variable bVar, VariableLayer bLayer) = EvaluationSeed.AddVariableLayer(db, baseline);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs);
        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, cVar, cLayer, t, -33.00, 151.00, 1.0);
        EvaluationSeed.AddGridcellDatum(db, bVar, bLayer, t, -33.00, 151.00, 1.0);
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -33.02, 151.01, 1.0); // within 10km
        EvaluationSeed.AddGridcellDatum(db, oVar, oLayer, t, -35.0, 149.0, 1.0); // outside 10km

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());
        await engine.ExecuteAsync(run.Id);

        EvaluationResult result = db.EvaluationResults
            .Include(r => r.Metrics)
            .Single(r => r.EvaluationRunId == run.Id);
        Assert.Contains(result.Metrics, m => m.MetricType == "n" && Math.Abs(m.Value - 1.0) < 0.001);
    }

    [Theory]
    [InlineData(AggregationLevel.Gridcell)]
    [InlineData(AggregationLevel.Stand)]
    [InlineData(AggregationLevel.Patch)]
    public async Task ExecuteAsync_LoadsAndMatchesByAggregationLevel(AggregationLevel level)
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db);
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, MatchingStrategy.ExactMatch);

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate, level);
        (Variable bVar, VariableLayer bLayer) = EvaluationSeed.AddVariableLayer(db, baseline, level);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs, level);

        AddDatumForLevel(db, level, cVar, cLayer, 2.0);
        AddDatumForLevel(db, level, bVar, bLayer, 2.0);
        AddDatumForLevel(db, level, oVar, oLayer, 2.0);

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());
        await engine.ExecuteAsync(run.Id);

        EvaluationResult result = db.EvaluationResults
            .Include(r => r.Metrics)
            .Single(r => r.EvaluationRunId == run.Id);
        Assert.Contains(result.Metrics, m => m.MetricType == "n" && Math.Abs(m.Value - 1.0) < 0.001);
    }

    [Fact]
    public async Task ExecuteAsync_IndividualLevel_UsesIndividualNumberInKey()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db);
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, MatchingStrategy.ExactMatch);

        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate, AggregationLevel.Individual);
        (Variable bVar, VariableLayer bLayer) = EvaluationSeed.AddVariableLayer(db, baseline, AggregationLevel.Individual);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs, AggregationLevel.Individual);

        Individual cInd = EvaluationSeed.EnsureIndividual(db, candidate, individualNumber: 42);
        Individual bInd = EvaluationSeed.EnsureIndividual(db, baseline, individualNumber: 42);
        Individual oInd = EvaluationSeed.EnsureIndividual(db, obs, individualNumber: 42);

        DateTime t = new(2025, 1, 1);
        EvaluationSeed.AddIndividualDatum(db, cVar, cLayer, cInd, t, -33, 151, 1.2);
        EvaluationSeed.AddIndividualDatum(db, bVar, bLayer, bInd, t, -33, 151, 1.2);
        EvaluationSeed.AddIndividualDatum(db, oVar, oLayer, oInd, t, -33, 151, 1.2);

        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate, baseline.Id);
        db.ChangeTracker.Clear();
        EvaluationEngine engine = new(db, Mock.Of<ILogger<EvaluationEngine>>());
        await engine.ExecuteAsync(run.Id);

        EvaluationResult result = db.EvaluationResults
            .Include(r => r.Metrics)
            .Single(r => r.EvaluationRunId == run.Id);
        Assert.Contains(result.Metrics, m => m.MetricType == "n" && Math.Abs(m.Value - 1.0) < 0.001);
    }

    private static void AddDatumForLevel(
        BenchmarksDbContext db,
        AggregationLevel level,
        Variable variable,
        VariableLayer layer,
        double value)
    {
        DateTime t = new(2025, 1, 1);
        switch (level)
        {
            case AggregationLevel.Gridcell:
                db.GridcellData.Add(new GridcellDatum
                {
                    VariableId = variable.Id,
                    LayerId = layer.Id,
                    Timestamp = t,
                    Latitude = -33,
                    Longitude = 151,
                    Value = value
                });
                break;
            case AggregationLevel.Stand:
                db.StandData.Add(new StandDatum
                {
                    VariableId = variable.Id,
                    LayerId = layer.Id,
                    Timestamp = t,
                    Latitude = -33,
                    Longitude = 151,
                    StandId = 1,
                    Value = value
                });
                break;
            case AggregationLevel.Patch:
                db.PatchData.Add(new PatchDatum
                {
                    VariableId = variable.Id,
                    LayerId = layer.Id,
                    Timestamp = t,
                    Latitude = -33,
                    Longitude = 151,
                    StandId = 1,
                    PatchId = 1,
                    Value = value
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }

        db.SaveChanges();
    }
}
