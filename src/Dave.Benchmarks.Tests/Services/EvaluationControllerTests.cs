using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Tests.Helpers;
using Dave.Benchmarks.Web.Controllers;
using Dave.Benchmarks.Web.Models;
using Dave.Benchmarks.Web.Services.Evaluation;
using LpjGuess.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class EvaluationControllerTests
{
    [Fact]
    public async Task Run_ReturnsNotFound_WhenCandidateMissing()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        var queue = new Mock<IEvaluationJobQueue>();
        EvaluationController controller = new(db, queue.Object);

        ActionResult<object> response = await controller.Run(new CreateEvaluationRunRequest
        {
            CandidateDatasetId = 999,
            MergeRequestId = "1",
            SourceBranch = "feature",
            TargetBranch = "main",
            CommitSha = "abc"
        }, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    [Fact]
    public async Task Run_CreatesPendingRun_AndEnqueuesId()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db, "sim", "chan");

        int enqueuedId = -1;
        var queue = new Mock<IEvaluationJobQueue>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, CancellationToken>((id, _) => enqueuedId = id)
            .Returns(ValueTask.CompletedTask);

        EvaluationController controller = new(db, queue.Object);

        ActionResult<object> response = await controller.Run(new CreateEvaluationRunRequest
        {
            CandidateDatasetId = candidate.Id,
            MergeRequestId = "2",
            SourceBranch = "feature/x",
            TargetBranch = "main",
            CommitSha = "abcdef"
        }, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.NotNull(ok.Value);
        EvaluationRun run = db.EvaluationRuns.Single();
        Assert.Equal(EvaluationRunStatus.Pending, run.Status);
        Assert.Equal(candidate.SimulationId, run.SimulationId);
        Assert.Equal(candidate.BaselineChannel, run.BaselineChannel);
        Assert.Equal(run.Id, enqueuedId);
    }

    [Fact]
    public async Task GetRun_ReturnsNotFound_WhenMissing()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        EvaluationController controller = new(db, Mock.Of<IEvaluationJobQueue>());

        ActionResult<EvaluationRun> response = await controller.GetRun(42, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    [Fact]
    public async Task GetRun_ReturnsRun_WithResultsAndMetrics()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

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
        db.EvaluationMetrics.Add(new EvaluationMetric
        {
            EvaluationResultId = result.Id,
            MetricType = "n",
            Value = 1
        });
        db.SaveChanges();

        EvaluationController controller = new(db, Mock.Of<IEvaluationJobQueue>());
        ActionResult<EvaluationRun> response = await controller.GetRun(run.Id, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(response.Result);
        EvaluationRun returned = Assert.IsType<EvaluationRun>(ok.Value);
        Assert.Single(returned.Results);
        Assert.Single(returned.Results.Single().Metrics);
    }

    [Fact]
    public async Task AcceptBaseline_ReturnsNotFound_WhenPredictionDatasetMissing()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        EvaluationController controller = new(db, Mock.Of<IEvaluationJobQueue>());

        ActionResult response = await controller.AcceptBaseline(new AcceptPredictionBaselineRequest
        {
            DatasetId = 999,
            AcceptedBy = "ci"
        }, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task AcceptBaseline_AppendsHistoryRows()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db, "sim", "main");
        EvaluationController controller = new(db, Mock.Of<IEvaluationJobQueue>());

        await controller.AcceptBaseline(new AcceptPredictionBaselineRequest
        {
            DatasetId = dataset.Id,
            AcceptedBy = "ci",
            AcceptedReason = "first",
            AcceptedFromPipelineId = "100"
        }, CancellationToken.None);

        await controller.AcceptBaseline(new AcceptPredictionBaselineRequest
        {
            DatasetId = dataset.Id,
            AcceptedBy = "ci",
            AcceptedReason = "second",
            AcceptedFromPipelineId = "101"
        }, CancellationToken.None);

        List<PredictionBaselineRegistryEntry> rows = db.PredictionBaselineRegistryEntries
            .Where(e => e.SimulationId == "sim" && e.BaselineChannel == "main")
            .OrderBy(e => e.AcceptedAt)
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("ci", rows[0].AcceptedBy);
        Assert.Equal("ci", rows[1].AcceptedBy);
        Assert.Equal("101", rows[1].AcceptedFromPipelineId);
    }
}
