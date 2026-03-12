using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Tests.Helpers;
using LpjGuess.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Tests.Data;

public class BenchmarksDbContextEvaluationConstraintsTests
{
    [Fact]
    public void SaveChanges_RejectsUnknownMetricType()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        db.EvaluationMetrics.Add(new EvaluationMetric
        {
            EvaluationResultId = 123,
            MetricType = "unknown",
            Value = 1.0
        });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("Unknown metric key", ex.Message);
    }

    [Fact]
    public void SaveChanges_TrimsKnownMetricType()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        EvaluationResult result = CreateValidEvaluationResult(db);

        EvaluationMetric metric = new()
        {
            EvaluationResultId = result.Id,
            MetricType = "  r2  ",
            Value = 0.8
        };

        db.EvaluationMetrics.Add(metric);
        db.SaveChanges();

        Assert.Equal("r2", metric.MetricType);
    }

    [Fact]
    public void EvaluationResult_RequiresValidObservationVariableAndLayer()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate);
        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);

        EvaluationResult result = new()
        {
            EvaluationRunId = run.Id,
            CandidateVariableId = cVar.Id,
            CandidateLayerId = cLayer.Id,
            ObservationVariableId = 99999,
            ObservationLayerId = 99999
        };

        db.EvaluationResults.Add(result);
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void EvaluationResult_EnforcesBaselineVariableLayerNullPair()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        EvaluationResult result = CreateValidEvaluationResult(db);

        PredictionDataset baseline = EvaluationSeed.CreatePredictionDataset(db, name: "baseline");
        (_, VariableLayer baselineLayer) = EvaluationSeed.AddVariableLayer(db, baseline, variableName: "lai", layerName: "total");

        result.BaselineVariableId = null;
        result.BaselineLayerId = baselineLayer.Id;
        db.Update(result);

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void EvaluationResult_EnforcesLayerBelongsToReferencedVariable()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate);
        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);

        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db);
        (Variable obsVarA, _) = EvaluationSeed.AddVariableLayer(db, obs, variableName: "lai", layerName: "a");
        (_, VariableLayer obsLayerB) = EvaluationSeed.AddVariableLayer(db, obs, variableName: "lai2", layerName: "b");

        EvaluationResult result = new()
        {
            EvaluationRunId = run.Id,
            CandidateVariableId = cVar.Id,
            CandidateLayerId = cLayer.Id,
            ObservationVariableId = obsVarA.Id,
            ObservationLayerId = obsLayerB.Id // mismatched variable/layer pair
        };

        db.EvaluationResults.Add(result);
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void EvaluationMetric_EnforcesUniqueMetricTypePerResult()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        EvaluationResult result = CreateValidEvaluationResult(db);

        db.EvaluationMetrics.Add(new EvaluationMetric { EvaluationResultId = result.Id, MetricType = "n", Value = 3 });
        db.EvaluationMetrics.Add(new EvaluationMetric { EvaluationResultId = result.Id, MetricType = "n", Value = 4 });

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void EvaluationMetric_RejectsNonFiniteValues(double value)
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        EvaluationResult result = CreateValidEvaluationResult(db);

        db.EvaluationMetrics.Add(new EvaluationMetric
        {
            EvaluationResultId = result.Id,
            MetricType = "r2",
            Value = value
        });

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void PredictionBaselineRegistry_AllowsHistoryRowsForSameScope()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset p1 = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "p1");
        PredictionDataset p2 = EvaluationSeed.CreatePredictionDataset(db, "sim", "main", "p2");

        db.PredictionBaselineRegistryEntries.Add(new PredictionBaselineRegistryEntry
        {
            SimulationId = "sim",
            BaselineChannel = "main",
            PredictionDatasetId = p1.Id,
            AcceptedAt = DateTime.UtcNow.AddMinutes(-1),
            AcceptedBy = "ci"
        });
        db.PredictionBaselineRegistryEntries.Add(new PredictionBaselineRegistryEntry
        {
            SimulationId = "sim",
            BaselineChannel = "main",
            PredictionDatasetId = p2.Id,
            AcceptedAt = DateTime.UtcNow,
            AcceptedBy = "ci"
        });

        db.SaveChanges();

        int count = db.PredictionBaselineRegistryEntries
            .Count(e => e.SimulationId == "sim" && e.BaselineChannel == "main");
        Assert.Equal(2, count);
    }

    private static EvaluationResult CreateValidEvaluationResult(BenchmarksDbContext db)
    {
        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate);
        (Variable cVar, VariableLayer cLayer) = EvaluationSeed.AddVariableLayer(db, candidate);

        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db);
        (Variable oVar, VariableLayer oLayer) = EvaluationSeed.AddVariableLayer(db, obs);

        EvaluationResult result = new()
        {
            EvaluationRunId = run.Id,
            CandidateVariableId = cVar.Id,
            CandidateLayerId = cLayer.Id,
            ObservationVariableId = oVar.Id,
            ObservationLayerId = oLayer.Id
        };

        db.EvaluationResults.Add(result);
        db.SaveChanges();
        return result;
    }
}
