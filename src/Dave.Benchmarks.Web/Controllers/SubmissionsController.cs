using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize(Policy = AuthorizationPolicies.GitLabCi)]
public class SubmissionsController : ControllerBase
{
    private readonly BenchmarksDbContext db;
    public SubmissionsController(BenchmarksDbContext db) => this.db = db;

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBenchmarkSubmissionRequest request, CancellationToken token)
    {
        string projectId = User.FindFirst("project_id")?.Value
            ?? throw new InvalidOperationException("Authenticated token has no project_id claim");
        string? tokenSha = User.FindFirst("sha")?.Value;
        if (tokenSha != null && !tokenSha.Equals(request.CommitSha, StringComparison.OrdinalIgnoreCase))
            return BadRequest("CommitSha does not match the authenticated GitLab job token");

        BenchmarkSubmission submission = new()
        {
            GitLabProjectId = projectId,
            MergeRequestId = request.MergeRequestId,
            PipelineId = request.PipelineId,
            CommitSha = request.CommitSha,
            CommitMessage = request.CommitMessage,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            BenchmarkName = request.BenchmarkName,
            Status = BenchmarkSubmissionStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        db.BenchmarkSubmissions.Add(submission);
        await db.SaveChangesAsync(token);
        return Ok(submission.Id);
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult> Complete(int id, CancellationToken token)
    {
        BenchmarkSubmission? submission = await db.BenchmarkSubmissions
            .Include(s => s.Datasets).FirstOrDefaultAsync(s => s.Id == id, token);
        if (submission == null) return NotFound();
        string? projectId = User.FindFirst("project_id")?.Value;
        if (projectId != null && submission.GitLabProjectId != projectId) return Forbid();
        if (submission.Status != BenchmarkSubmissionStatus.Open)
            return BadRequest("Only open submissions can be completed");
        if (submission.Datasets.Count == 0)
            return BadRequest("A submission must contain at least one dataset");
        submission.Status = BenchmarkSubmissionStatus.Complete;
        submission.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return Ok();
    }

    [HttpPost("{id}/fail")]
    public async Task<ActionResult> Fail(int id, [FromBody] string? error, CancellationToken token)
    {
        BenchmarkSubmission? submission = await db.BenchmarkSubmissions.FindAsync([id], token);
        if (submission == null) return NotFound();
        string? projectId = User.FindFirst("project_id")?.Value;
        if (projectId != null && submission.GitLabProjectId != projectId) return Forbid();
        submission.Status = BenchmarkSubmissionStatus.Failed;
        submission.CompletedAt = DateTime.UtcNow;
        submission.ErrorMessage = error;
        await db.SaveChangesAsync(token);
        return Ok();
    }
}
