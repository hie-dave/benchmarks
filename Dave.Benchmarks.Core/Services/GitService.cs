using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dave.Benchmarks.Core.Utilities;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.Core.Services;

public class GitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger;
    }

    public class RepositoryInfo
    {
        public string CommitHash { get; set; } = string.Empty;
        public byte[] Patches { get; set; } = Array.Empty<byte>();
        public string RepositoryPath { get; set; } = string.Empty;
        public bool HasUncommittedChanges { get; set; }
    }

    public RepositoryInfo GetRepositoryInfo(string repoPath)
    {
        using var _ = _logger.BeginScope("git");
        if (string.IsNullOrEmpty(repoPath))
        {
            throw new InvalidOperationException($"No git repository found in or above {repoPath}");
        }

        using var repo = new Repository(repoPath);
        
        // Check if repository is valid
        if (repo.Info.IsBare)
        {
            throw new InvalidOperationException("Cannot work with bare repositories");
        }

        // Check if HEAD exists and is valid
        if (repo.Head?.Tip == null)
        {
            throw new InvalidOperationException("Repository HEAD is invalid or repository has no commits");
        }

        // Get current commit hash
        var commitHash = repo.Head.Tip.Sha;

        // Check if HEAD is detached
        if (repo.Info.IsHeadDetached)
        {
            _logger.LogWarning("Repository HEAD is detached. This may affect reproducibility.");
        }

        // Get uncommitted changes as patches
        var patches = new List<string>();
        var diff = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, 
            DiffTargets.Index | DiffTargets.WorkingDirectory);

        var hasUncommittedChanges = false;
        foreach (var change in diff)
        {
            var patch = repo.Diff.Compare<Patch>(new[] { change.Path });
            if (!string.IsNullOrEmpty(patch.Content))
            {
                patches.Add(patch.Content);
                hasUncommittedChanges = true;
            }
        }

        if (hasUncommittedChanges)
        {
            _logger.LogWarning("Repository has uncommitted changes. These will be saved with the dataset for reproducibility.");
        }

        // Combine all patches into a single string and convert to bytes
        var allPatches = string.Join("\n", patches);
        return new RepositoryInfo
        {
            CommitHash = commitHash,
            Patches = CompressionUtility.CompressText(allPatches),
            RepositoryPath = repoPath,
            HasUncommittedChanges = hasUncommittedChanges
        };
    }

    public IEnumerable<(string SiteName, string InstructionFile, string OutputDirectory)> FindSiteLevelRuns(string repoPath)
    {
        var benchmarksPath = Path.Combine(repoPath, "benchmarks", "ozflux");
        if (!Directory.Exists(benchmarksPath))
        {
            yield break;
        }

        foreach (var siteDir in Directory.GetDirectories(benchmarksPath))
        {
            var siteName = Path.GetFileName(siteDir);
            var instructionFiles = Directory.GetFiles(siteDir, "*.ins");
            var outputDir = Path.Combine(siteDir, "out");

            if (instructionFiles.Length > 0 && Directory.Exists(outputDir))
            {
                // Use the first instruction file found (or you could add logic to determine the main one)
                yield return (siteName, instructionFiles[0], outputDir);
            }
        }
    }

    private string FindRepositoryPath(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);
        while (currentDir != null)
        {
            var gitDir = Path.Combine(currentDir.FullName, ".git");
            if (Directory.Exists(gitDir))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }
        return string.Empty;
    }
}
