using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Tests.Helpers;
using Dave.Benchmarks.Web.Controllers;
using Dave.Benchmarks.Web.Models;
using LpjGuess.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Dave.Benchmarks.Tests.Services;

public class EvaluationRunsControllerTests
{
    [Fact]
    public async Task Index_GroupsRunsByMergeRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate);

        ViewResult view = Assert.IsType<ViewResult>(await new EvaluationRunsController(db)
            .Index(null, null, CancellationToken.None));
        EvaluationRunIndexViewModel model = Assert.IsType<EvaluationRunIndexViewModel>(view.Model);

        MergeRequestSummaryViewModel mergeRequest = Assert.Single(model.MergeRequests);
        Assert.Equal("123", mergeRequest.MergeRequestId);
        Assert.Equal(run.Id, Assert.Single(Assert.Single(model.Submissions).Runs).Id);
    }

    [Fact]
    public async Task Details_MapsDatasetComparisonAndMetrics()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate);
        (Variable candidateVariable, VariableLayer candidateLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        ObservationDataset observation = EvaluationSeed.CreateObservationDataset(db);
        (Variable observationVariable, VariableLayer observationLayer) = EvaluationSeed.AddVariableLayer(db, observation);
        EvaluationResult result = new()
        {
            EvaluationRunDatasetId = run.Datasets.Single().Id,
            CandidateVariableId = candidateVariable.Id,
            CandidateLayerId = candidateLayer.Id,
            ObservationVariableId = observationVariable.Id,
            ObservationLayerId = observationLayer.Id
        };
        db.EvaluationResults.Add(result);
        db.SaveChanges();
        db.EvaluationMetrics.Add(new EvaluationMetric
        {
            EvaluationResultId = result.Id,
            MetricType = "n",
            Value = 12
        });
        db.SaveChanges();

        ViewResult view = Assert.IsType<ViewResult>(await new EvaluationRunsController(db)
            .Details(run.Id, CancellationToken.None));
        EvaluationRunDetailsViewModel model = Assert.IsType<EvaluationRunDetailsViewModel>(view.Model);

        EvaluationDatasetDetailsViewModel dataset = Assert.Single(model.Datasets);
        EvaluationResultDetailsViewModel comparison = Assert.Single(dataset.Results);
        Assert.Equal(observation.Name, comparison.ObservationDataset);
        Assert.Equal(12, Assert.Single(comparison.Metrics).Value);
    }
}
