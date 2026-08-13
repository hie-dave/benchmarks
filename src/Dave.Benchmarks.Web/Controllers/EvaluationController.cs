using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Web.Models;
using Dave.Benchmarks.Web.Services.Evaluation;
using LpjGuess.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.GitLabCi)]
public class EvaluationController : ControllerBase
{
    private readonly BenchmarksDbContext db;
    private readonly IEvaluationJobQueue queue;

    public EvaluationController(BenchmarksDbContext db, IEvaluationJobQueue queue)
    {
        this.db = db;
        this.queue = queue;
    }

    [HttpPost("run")]
    public async Task<ActionResult<object>> Run(
        [FromBody] CreateEvaluationRunRequest request,
        CancellationToken cancellationToken)
    {
        BenchmarkSubmission? submission = await db.BenchmarkSubmissions
            .Include(s => s.Datasets)
            .FirstOrDefaultAsync(s => s.Id == request.BenchmarkSubmissionId, cancellationToken);
        if (submission == null) return NotFound($"Submission {request.BenchmarkSubmissionId} not found");
        string? projectId = HttpContext?.User.FindFirst("project_id")?.Value;
        if (projectId != null && submission.GitLabProjectId != projectId) return Forbid();
        if (submission.Status != BenchmarkSubmissionStatus.Complete)
            return BadRequest("Only complete submissions can be evaluated");

        EvaluationRun run = new()
        {
            BenchmarkSubmissionId = submission.Id,
            Status = EvaluationRunStatus.Pending,
            StartedAt = DateTime.UtcNow
        };
        foreach (PredictionDataset dataset in submission.Datasets)
            run.Datasets.Add(new EvaluationRunDataset
            {
                CandidateDatasetId = dataset.Id,
                Status = EvaluationRunStatus.Pending
            });

        db.EvaluationRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(run.Id, cancellationToken);

        return Ok(new { evaluationRunId = run.Id });
    }

    [HttpGet("runs/{id}")]
    public async Task<ActionResult<EvaluationRun>> GetRun(int id, CancellationToken cancellationToken)
    {
        EvaluationRun? run = await db.EvaluationRuns
            .Include(r => r.BenchmarkSubmission)
            .Include(r => r.Datasets).ThenInclude(d => d.Results).ThenInclude(r => r.Metrics)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (run == null)
            return NotFound($"Evaluation run {id} not found");

        return Ok(run);
    }

    [HttpPost("accept")]
    [Authorize(Policy = AuthorizationPolicies.GitLabProtectedRef)]
    public async Task<ActionResult> AcceptBaseline(
        [FromBody] AcceptPredictionBaselineRequest request,
        CancellationToken cancellationToken)
    {
        PredictionDataset? dataset = await db.Datasets
            .OfType<PredictionDataset>()
            .FirstOrDefaultAsync(d => d.Id == request.DatasetId, cancellationToken);

        if (dataset == null)
            return NotFound($"Prediction dataset {request.DatasetId} not found");

        // Append-only baseline acceptance history; latest row in this scope is current baseline.
        var acceptance = new PredictionBaselineRegistryEntry
        {
            SimulationId = dataset.SimulationId,
            BaselineChannel = dataset.BaselineChannel,
            PredictionDatasetId = dataset.Id,
            AcceptedAt = DateTime.UtcNow,
            AcceptedBy = request.AcceptedBy,
            AcceptedReason = request.AcceptedReason,
            AcceptedFromPipelineId = request.AcceptedFromPipelineId
        };

        db.PredictionBaselineRegistryEntries.Add(acceptance);

        await db.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}
