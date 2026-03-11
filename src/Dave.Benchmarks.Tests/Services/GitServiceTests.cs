using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dave.Benchmarks.Core.Services;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class GitServiceTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly GitService _service;
    private readonly Mock<ILogger<GitService>> _logger;

    public GitServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "git_tests");
        _logger = new Mock<ILogger<GitService>>();
        _service = new GitService(_logger.Object);
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_testDir, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public void GetRepositoryInfo_ValidRepo_Success()
    {
        // Arrange
        var repoPath = CreateTestRepository();
        CreateTestFile(repoPath, "test.txt", "initial content");
        CommitAll(repoPath, "Initial commit");

        // Act
        var info = _service.GetRepositoryInfo(repoPath);

        // Assert
        Assert.NotEmpty(info.CommitHash);
        Assert.Equal(repoPath, info.RepositoryPath);
        Assert.False(info.HasUncommittedChanges);
    }

    [Fact]
    public void GetRepositoryInfo_UncommittedChanges_DetectsChanges()
    {
        // Arrange
        var repoPath = CreateTestRepository();
        CreateTestFile(repoPath, "test.txt", "initial content");
        CommitAll(repoPath, "Initial commit");
        CreateTestFile(repoPath, "test.txt", "modified content");

        // Act
        var info = _service.GetRepositoryInfo(repoPath);

        // Assert
        Assert.True(info.HasUncommittedChanges);
        Assert.NotEmpty(info.Patches);
    }

    [Fact]
    public void GetRepositoryInfo_DetachedHead_LogsWarning()
    {
        // Arrange
        var repoPath = CreateTestRepository();
        CreateTestFile(repoPath, "test.txt", "initial content");
        var repo = new Repository(repoPath);
        var commit = CommitAll(repoPath, "Initial commit");
        
        // Detach HEAD
        using (var repo2 = new Repository(repoPath))
        {
            Commands.Checkout(repo2, commit);
        }

        // Act
        var info = _service.GetRepositoryInfo(repoPath);

        // Assert
        _logger.Verify(
            x => x.Log(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("detached")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void FindSiteLevelRuns_ValidStructure_FindsRuns()
    {
        // Arrange
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

        // Act
        var runs = _service.FindSiteLevelRuns(repoPath).ToList();

        // Assert
        Assert.Equal(2, runs.Count);
        Assert.Contains(runs, r => r.SiteName == "site1");
        Assert.Contains(runs, r => r.SiteName == "site2");
    }

    private string CreateTestRepository()
    {
        var repoPath = Path.Combine(_testDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(repoPath);
        Repository.Init(repoPath);
        
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
