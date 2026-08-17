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
        Assert.Equal(result.Id, comparison.Id);
        Assert.Equal(observation.Name, comparison.ObservationDataset);
        Assert.Equal(12, Assert.Single(comparison.Metrics).Value);
        Assert.Equal(1, model.ComparisonCount);
        Assert.Contains("n", model.MetricTypes);
    }

    [Fact]
    public async Task Comparison_LoadsSeriesAndPairsMatchingTimestamps()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset candidate = EvaluationSeed.CreatePredictionDataset(db, name: "AU-Tum");
        EvaluationRun run = EvaluationSeed.CreateRun(db, candidate);
        (Variable candidateVariable, VariableLayer candidateLayer) = EvaluationSeed.AddVariableLayer(db, candidate);
        ObservationDataset observation = EvaluationSeed.CreateObservationDataset(db);
        (Variable observationVariable, VariableLayer observationLayer) = EvaluationSeed.AddVariableLayer(db, observation);
        DateTime common = new(2025, 1, 1);
        EvaluationSeed.AddGridcellDatum(db, candidateVariable, candidateLayer, common, -35, 149, 2);
        EvaluationSeed.AddGridcellDatum(db, candidateVariable, candidateLayer, common.AddDays(1), -35, 149, 3);
        EvaluationSeed.AddGridcellDatum(db, observationVariable, observationLayer, common, -35, 149, 1);
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

        ViewResult view = Assert.IsType<ViewResult>(await new EvaluationRunsController(db)
            .Comparison(result.Id, CancellationToken.None));
        EvaluationComparisonViewModel model = Assert.IsType<EvaluationComparisonViewModel>(view.Model);

        Assert.Equal(run.Id, model.EvaluationRunId);
        Assert.Equal(2, model.CandidatePoints.Count);
        Assert.Single(model.ObservationPoints);
        EvaluationPairedPoint pair = Assert.Single(model.PairedPoints);
        Assert.Equal(1, pair.Observed);
        Assert.Equal(2, pair.Predicted);
    }
}
