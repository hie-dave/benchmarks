using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

/// <summary>Read-only browser for merge-request evaluation history.</summary>
public class EvaluationRunsController : Controller
{
    private readonly BenchmarksDbContext db;

    public EvaluationRunsController(BenchmarksDbContext db)
    {
        this.db = db;
    }

    public async Task<IActionResult> Index(
        string? projectId,
        string? mergeRequestId,
        CancellationToken cancellationToken)
    {
        List<BenchmarkSubmission> all = await db.BenchmarkSubmissions
            .AsNoTracking()
            .Include(s => s.EvaluationRuns)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        List<MergeRequestSummaryViewModel> mergeRequests = all
            .GroupBy(s => new { s.GitLabProjectId, s.MergeRequestId })
            .Select(group =>
            {
                BenchmarkSubmission latestSubmission = group.OrderByDescending(s => s.CreatedAt).First();
                EvaluationRun? latestRun = group.SelectMany(s => s.EvaluationRuns)
                    .OrderByDescending(r => r.StartedAt).FirstOrDefault();
                return new MergeRequestSummaryViewModel
                {
                    ProjectId = group.Key.GitLabProjectId,
                    MergeRequestId = group.Key.MergeRequestId,
                    SourceBranch = latestSubmission.SourceBranch,
                    TargetBranch = latestSubmission.TargetBranch,
                    LastTestedAt = latestSubmission.CreatedAt,
                    SubmissionCount = group.Count(),
                    RunCount = group.Sum(s => s.EvaluationRuns.Count),
                    LatestPassed = latestRun?.Passed,
                    LatestStatus = latestRun?.Status
                };
            })
            .OrderByDescending(mr => mr.LastTestedAt)
            .ToList();

        MergeRequestSummaryViewModel? selected = mergeRequests.FirstOrDefault(mr =>
            mr.ProjectId == projectId && mr.MergeRequestId == mergeRequestId) ?? mergeRequests.FirstOrDefault();
        List<SubmissionRunsViewModel> submissions = selected == null ? [] : all
            .Where(s => s.GitLabProjectId == selected.ProjectId && s.MergeRequestId == selected.MergeRequestId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubmissionRunsViewModel
            {
                SubmissionId = s.Id,
                PipelineId = s.PipelineId,
                CommitSha = s.CommitSha,
                CommitMessage = s.CommitMessage,
                BenchmarkName = s.BenchmarkName,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                Runs = s.EvaluationRuns.OrderByDescending(r => r.StartedAt)
                    .Select(r => new EvaluationRunSummaryViewModel
                    {
                        Id = r.Id,
                        Status = r.Status,
                        Passed = r.Passed,
                        StartedAt = r.StartedAt,
                        CompletedAt = r.CompletedAt
                    }).ToList()
            }).ToList();

        return View(new EvaluationRunIndexViewModel
        {
            MergeRequests = mergeRequests,
            SelectedProjectId = selected?.ProjectId,
            SelectedMergeRequestId = selected?.MergeRequestId,
            Submissions = submissions
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        EvaluationRun? run = await db.EvaluationRuns
            .AsNoTracking()
            .Include(r => r.BenchmarkSubmission)
            .Include(r => r.Datasets).ThenInclude(d => d.CandidateDataset)
            .Include(r => r.Datasets).ThenInclude(d => d.BaselineDataset)
            .Include(r => r.Datasets).ThenInclude(d => d.Results).ThenInclude(r => r.CandidateVariable)
            .Include(r => r.Datasets).ThenInclude(d => d.Results).ThenInclude(r => r.CandidateLayer)
            .Include(r => r.Datasets).ThenInclude(d => d.Results).ThenInclude(r => r.ObservationVariable)
                .ThenInclude(v => v.Dataset)
            .Include(r => r.Datasets).ThenInclude(d => d.Results).ThenInclude(r => r.ObservationLayer)
            .Include(r => r.Datasets).ThenInclude(d => d.Results).ThenInclude(r => r.Metrics)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run == null) return NotFound();

        int[] baselineDatasetIds = run.Datasets.Where(d => d.BaselineDatasetId.HasValue)
            .Select(d => d.BaselineDatasetId!.Value).Distinct().ToArray();
        List<EvaluationResult> baselineResults = baselineDatasetIds.Length == 0 ? [] : await db.EvaluationResults
            .AsNoTracking()
            .Include(r => r.Metrics)
            .Include(r => r.EvaluationRunDataset).ThenInclude(d => d.EvaluationRun)
            .Where(r => baselineDatasetIds.Contains(r.EvaluationRunDataset.CandidateDatasetId) &&
                        r.EvaluationRunDataset.EvaluationRunId != run.Id &&
                        r.EvaluationRunDataset.Status == EvaluationRunStatus.Succeeded)
            .OrderByDescending(r => r.EvaluationRunDataset.EvaluationRun.StartedAt)
            .ToListAsync(cancellationToken);

        bool? Unchanged(EvaluationRunDataset dataset, EvaluationResult result)
        {
            if (!dataset.BaselineDatasetId.HasValue || !result.BaselineVariableId.HasValue ||
                !result.BaselineLayerId.HasValue) return null;
            EvaluationResult? previous = baselineResults.FirstOrDefault(b =>
                b.EvaluationRunDataset.CandidateDatasetId == dataset.BaselineDatasetId &&
                b.CandidateVariableId == result.BaselineVariableId &&
                b.CandidateLayerId == result.BaselineLayerId &&
                b.ObservationVariableId == result.ObservationVariableId &&
                b.ObservationLayerId == result.ObservationLayerId);
            if (previous == null) return null;
            return result.Metrics.Count > 0 && result.Metrics.All(metric =>
                previous.Metrics.Any(old => old.MetricType == metric.MetricType &&
                    Math.Abs(old.Value - metric.Value) <= 1e-12 * Math.Max(1, Math.Abs(old.Value))));
        }

        BenchmarkSubmission submission = run.BenchmarkSubmission;
        List<EvaluationDatasetDetailsViewModel> datasets = run.Datasets.OrderBy(d => d.CandidateDataset.Name).Select(d =>
            new EvaluationDatasetDetailsViewModel
            {
                CandidateDatasetId = d.CandidateDatasetId,
                CandidateName = d.CandidateDataset.Name,
                SimulationId = d.CandidateDataset.SimulationId,
                BaselineName = d.BaselineDataset?.Name,
                Status = d.Status,
                Passed = d.Passed,
                ErrorMessage = d.ErrorMessage,
                Results = d.Results
                    .OrderBy(r => r.CandidateVariable.Name).ThenBy(r => r.CandidateLayer.Name)
                    .Select(r => new EvaluationResultDetailsViewModel
                    {
                        Id = r.Id,
                        CandidateVariable = r.CandidateVariable.Name,
                        CandidateLayer = r.CandidateLayer.Name,
                        ObservationDataset = r.ObservationVariable.Dataset.Name,
                        ObservationVariable = r.ObservationVariable.Name,
                        ObservationLayer = r.ObservationLayer.Name,
                        MetricsUnchangedFromBaseline = Unchanged(d, r),
                        Metrics = r.Metrics.OrderBy(m => m.MetricType).Select(m =>
                            new EvaluationMetricViewModel { Type = m.MetricType, Value = m.Value }).ToList()
                    }).ToList()
            }).ToList();
        return View(new EvaluationRunDetailsViewModel
        {
            Id = run.Id,
            Status = run.Status,
            Passed = run.Passed,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            ErrorMessage = run.ErrorMessage,
            SubmissionId = submission.Id,
            ProjectId = submission.GitLabProjectId,
            MergeRequestId = submission.MergeRequestId,
            PipelineId = submission.PipelineId,
            CommitSha = submission.CommitSha,
            CommitMessage = submission.CommitMessage,
            SourceBranch = submission.SourceBranch,
            TargetBranch = submission.TargetBranch,
            BenchmarkName = submission.BenchmarkName,
            Datasets = datasets,
            ComparisonCount = datasets.Sum(d => d.Results.Count),
            DatasetCountWithoutBaseline = datasets.Count(d => d.BaselineName == null),
            UnchangedComparisonCount = datasets.SelectMany(d => d.Results)
                .Count(r => r.MetricsUnchangedFromBaseline == true),
            MetricTypes = datasets.SelectMany(d => d.Results).SelectMany(r => r.Metrics)
                .Select(m => m.Type).Distinct().Order().ToList()
        });
    }

    public async Task<IActionResult> Comparison(int id, CancellationToken cancellationToken)
    {
        EvaluationResult? result = await db.EvaluationResults
            .AsNoTracking()
            .Include(r => r.EvaluationRunDataset).ThenInclude(d => d.CandidateDataset)
            .Include(r => r.CandidateVariable)
            .Include(r => r.CandidateLayer)
            .Include(r => r.ObservationVariable).ThenInclude(v => v.Dataset)
            .Include(r => r.ObservationLayer)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (result == null) return NotFound();
        if (result.CandidateVariable.Level != LpjGuess.Core.Models.Entities.AggregationLevel.Gridcell ||
            result.ObservationVariable.Level != LpjGuess.Core.Models.Entities.AggregationLevel.Gridcell)
            return BadRequest("Only gridcell-level evaluation comparisons can be plotted");

        List<EvaluationChartPoint> candidate = await db.GridcellData.AsNoTracking()
            .Where(d => d.VariableId == result.CandidateVariableId && d.LayerId == result.CandidateLayerId)
            .OrderBy(d => d.Timestamp)
            .Select(d => new EvaluationChartPoint(d.Timestamp, d.Value))
            .ToListAsync(cancellationToken);
        List<EvaluationChartPoint> observation = await db.GridcellData.AsNoTracking()
            .Where(d => d.VariableId == result.ObservationVariableId && d.LayerId == result.ObservationLayerId)
            .OrderBy(d => d.Timestamp)
            .Select(d => new EvaluationChartPoint(d.Timestamp, d.Value))
            .ToListAsync(cancellationToken);

        Dictionary<DateTime, List<double>> candidateByTime = candidate.GroupBy(p => p.Timestamp)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Value).ToList());
        List<EvaluationPairedPoint> pairs = [];
        foreach (IGrouping<DateTime, EvaluationChartPoint> observedAtTime in observation.GroupBy(p => p.Timestamp))
        {
            if (!candidateByTime.TryGetValue(observedAtTime.Key, out List<double>? predicted)) continue;
            List<double> observed = observedAtTime.Select(p => p.Value).ToList();
            for (int i = 0; i < Math.Min(observed.Count, predicted.Count); i++)
                pairs.Add(new EvaluationPairedPoint(observedAtTime.Key, observed[i], predicted[i]));
        }

        return View(new EvaluationComparisonViewModel
        {
            ResultId = result.Id,
            EvaluationRunId = result.EvaluationRunDataset.EvaluationRunId,
            Site = result.EvaluationRunDataset.CandidateDataset.Name,
            CandidateVariable = result.CandidateVariable.Name,
            CandidateLayer = result.CandidateLayer.Name,
            ObservationVariable = result.ObservationVariable.Name,
            ObservationLayer = result.ObservationLayer.Name,
            Units = result.CandidateVariable.Units,
            CandidatePoints = candidate,
            ObservationPoints = observation,
            PairedPoints = pairs
        });
    }
}
