using System.Security.Claims;
using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Tests.Helpers;
using Dave.Benchmarks.Web.Controllers;
using Dave.Benchmarks.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dave.Benchmarks.Tests.Services;

public class SubmissionsControllerTests
{
    [Fact]
    public async Task Create_UsesAuthenticatedProjectAndStoresCommitMetadata()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        SubmissionsController controller = Controller(db, "55", "abc");

        ActionResult<int> result = await controller.Create(new CreateBenchmarkSubmissionRequest
        {
            MergeRequestId = "7", PipelineId = "99", CommitSha = "abc",
            CommitMessage = "test message", SourceBranch = "feature", TargetBranch = "main",
            BenchmarkName = "sites"
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        BenchmarkSubmission stored = Assert.Single(db.BenchmarkSubmissions);
        Assert.Equal("55", stored.GitLabProjectId);
        Assert.Equal("test message", stored.CommitMessage);
        Assert.Equal(BenchmarkSubmissionStatus.Open, stored.Status);
    }

    [Fact]
    public async Task Complete_RequiresAtLeastOneDataset()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        BenchmarkSubmission submission = new()
        {
            GitLabProjectId = "55", MergeRequestId = "7", PipelineId = "99", CommitSha = "abc",
            SourceBranch = "feature", TargetBranch = "main", BenchmarkName = "sites",
            Status = BenchmarkSubmissionStatus.Open, CreatedAt = DateTime.UtcNow
        };
        db.Add(submission); db.SaveChanges();

        ActionResult result = await Controller(db, "55", "abc").Complete(submission.Id, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static SubmissionsController Controller(BenchmarksDbContext db, string projectId, string sha)
    {
        ClaimsIdentity identity = new([new Claim("project_id", projectId), new Claim("sha", sha)], "test");
        return new SubmissionsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }
}
