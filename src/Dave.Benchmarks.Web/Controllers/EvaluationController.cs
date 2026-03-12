using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Web.Models;
using Dave.Benchmarks.Web.Services.Evaluation;
using LpjGuess.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        PredictionDataset? candidate = await db.Datasets
            .OfType<PredictionDataset>()
            .FirstOrDefaultAsync(d => d.Id == request.CandidateDatasetId, cancellationToken);

        if (candidate == null)
            return NotFound($"Candidate prediction dataset {request.CandidateDatasetId} not found");

        EvaluationRun run = new()
        {
            CandidateDatasetId = candidate.Id,
            SimulationId = candidate.SimulationId,
            BaselineChannel = candidate.BaselineChannel,
            MergeRequestId = request.MergeRequestId,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            CommitSha = request.CommitSha,
            Status = EvaluationRunStatus.Pending,
            StartedAt = DateTime.UtcNow
        };

        db.EvaluationRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(run.Id, cancellationToken);

        return Ok(new { evaluationRunId = run.Id });
    }

    [HttpGet("runs/{id}")]
    public async Task<ActionResult<EvaluationRun>> GetRun(int id, CancellationToken cancellationToken)
    {
        EvaluationRun? run = await db.EvaluationRuns
            .Include(r => r.Results)
                .ThenInclude(r => r.Metrics)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (run == null)
            return NotFound($"Evaluation run {id} not found");

        return Ok(run);
    }

    [HttpPost("accept")]
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
