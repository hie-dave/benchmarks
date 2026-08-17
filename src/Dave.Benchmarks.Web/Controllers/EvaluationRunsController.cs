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

        BenchmarkSubmission submission = run.BenchmarkSubmission;
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
            Datasets = run.Datasets.OrderBy(d => d.CandidateDataset.Name).Select(d =>
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
                            CandidateVariable = r.CandidateVariable.Name,
                            CandidateLayer = r.CandidateLayer.Name,
                            ObservationDataset = r.ObservationVariable.Dataset.Name,
                            ObservationVariable = r.ObservationVariable.Name,
                            ObservationLayer = r.ObservationLayer.Name,
                            Metrics = r.Metrics.OrderBy(m => m.MetricType).Select(m =>
                                new EvaluationMetricViewModel { Type = m.MetricType, Value = m.Value }).ToList()
                        }).ToList()
                }).ToList()
        });
    }
}
