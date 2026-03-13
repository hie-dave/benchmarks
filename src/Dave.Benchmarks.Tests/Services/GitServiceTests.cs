using System;
using System.IO;
using System.Linq;
using Dave.Benchmarks.Core.Services;
using Dave.Benchmarks.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class GitServiceTests : IDisposable
{
    private readonly TempDirectory _testDir;
    private readonly GitService _service;
    private readonly Mock<ILogger<GitService>> _logger;

    public GitServiceTests()
    {
        _testDir = TempDirectory.Create("git_tests_");
        _logger = new Mock<ILogger<GitService>>();
        _service = new GitService(_logger.Object);
    }

    public void Dispose()
    {
        _testDir.Dispose();
    }

    [Fact]
    public void GetRepositoryInfo_ValidRepo_Success()
    {
        var repoPath = CreateTestRepository();
        CreateTestFile(repoPath, "test.txt", "initial content");
        CommitAll(repoPath, "Initial commit");

        var info = _service.GetRepositoryInfo(repoPath);

        Assert.NotEmpty(info.CommitHash);
        Assert.Equal(repoPath, info.RepositoryPath);
        Assert.False(info.HasUncommittedChanges);
        Assert.False(string.IsNullOrWhiteSpace(info.BranchName));
    }

    [Fact]
    public void GetRepositoryInfo_UncommittedChanges_DetectsChanges()
    {
        var repoPath = CreateTestRepository();
        CreateTestFile(repoPath, "test.txt", "initial content");
        CommitAll(repoPath, "Initial commit");
        CreateTestFile(repoPath, "test.txt", "modified content");

        var info = _service.GetRepositoryInfo(repoPath);

        Assert.True(info.HasUncommittedChanges);
        Assert.NotEmpty(info.Patches);
    }

    [Fact]
    public void GetRepositoryInfo_DetachedHead_LogsWarning()
    {
        var repoPath = CreateTestRepository();
        CreateTestFile(repoPath, "test.txt", "initial content");
        var commit = CommitAll(repoPath, "Initial commit");
        
        // Detach HEAD
        using (var repo2 = new Repository(repoPath))
        {
            Commands.Checkout(repo2, commit);
        }

        var info = _service.GetRepositoryInfo(repoPath);

        _logger.Verify(
            x => x.Log(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("detached")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.Null(info.BranchName);
    }

    [Fact]
    public void FindSiteLevelRuns_ValidStructure_FindsRuns()
    {
        var repoPath = CreateTestRepository();
        var ozfluxPath = Path.Combine(repoPath, "benchmarks", "ozflux");
        var site1Path = Path.Combine(ozfluxPath, "site1");
        var site2Path = Path.Combine(ozfluxPath, "site2");

        Directory.CreateDirectory(ozfluxPath);
        Directory.CreateDirectory(site1Path);
        Directory.CreateDirectory(site2Path);
        Directory.CreateDirectory(Path.Combine(site1Path, "out"));
        Directory.CreateDirectory(Path.Combine(site2Path, "out"));

        CreateTestFile(site1Path, "run.ins", "param = value");
        CreateTestFile(site2Path, "run.ins", "param = value");

        var runs = _service.FindSiteLevelRuns(repoPath).ToList();

        Assert.Equal(2, runs.Count);
        Assert.Contains(runs, r => r.SiteName == "site1");
        Assert.Contains(runs, r => r.SiteName == "site2");
    }

    [Fact]
    public void FindSiteLevelRuns_InvalidStructure_ReturnsEmpty()
    {
        var repoPath = CreateTestRepository();
        var runs = _service.FindSiteLevelRuns(repoPath).ToList();
        Assert.Empty(runs);
    }

    [Fact]
    public void GetRepositoryInfo_EmptyPath_ThrowsException()
    {
        Exception ex = Assert.Throws<ArgumentException>(() => _service.GetRepositoryInfo(""));
        Assert.Contains("path", ex.Message);
    }

    [Fact]
    public void GetRepositoryInfo_NonExistentPath_ThrowsException()
    {
        string nonExistentPath = Path.Combine(_testDir.AbsolutePath, "nonexistent");
        Exception ex = Assert.Throws<ArgumentException>(() => _service.GetRepositoryInfo(nonExistentPath));
        Assert.Contains("path", ex.Message);
    }

    [Fact]
    public void GetRepositoryInfo_BareRepository_ThrowsException()
    {
        var repoPath = CreateTestRepository(bare: true);

        Exception ex = Assert.Throws<InvalidOperationException>(() => _service.GetRepositoryInfo(repoPath));
        Assert.Contains("bare", ex.Message);
    }

    [Fact]
    public void GetRepositoryInfo_NoCommits_ThrowsException()
    {
        var repoPath = CreateTestRepository();

        Exception ex = Assert.Throws<InvalidOperationException>(() => _service.GetRepositoryInfo(repoPath));
        Assert.Contains("HEAD", ex.Message);
    }

    private string CreateTestRepository(bool bare = false)
    {
        var repoPath = Path.Combine(_testDir.AbsolutePath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(repoPath);
        Repository.Init(repoPath, bare);

        using var repo = new Repository(repoPath);
        var author = new Signature("test", "test@test.com", DateTimeOffset.Now);
        repo.Config.Set("user.name", "test");
        repo.Config.Set("user.email", "test@test.com");
        
        return repoPath;
    }

    private void CreateTestFile(string basePath, string fileName, string content)
    {
        var filePath = Path.Combine(basePath, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, content);
    }

    private string CommitAll(string repoPath, string message)
    {
        using var repo = new Repository(repoPath);
        Commands.Stage(repo, "*");
        var author = new Signature("test", "test@test.com", DateTimeOffset.Now);
        var commit = repo.Commit(message, author, author);
        return commit.Sha;
    }
}
