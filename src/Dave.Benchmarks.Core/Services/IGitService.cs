namespace Dave.Benchmarks.Core.Services;

/// <summary>
/// Interface to git operations.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Get information about the git repository at the specified path,
    /// including current commit hash and any uncommitted changes.
    /// </summary>
    /// <param name="repoPath">Path to the git repository.</param>
    /// <returns>Information about the git repository.</returns>
    public RepositoryInfo GetRepositoryInfo(string repoPath);

    /// <summary>
    /// Find site-level runs in the specified repository. This looks for
    /// directories under "benchmarks/ozflux" that contain instruction files and
    /// output directories.
    /// </summary>
    /// <param name="repoPath">Path to the git repository.</param>
    /// <returns>Collection of site-level runs, each represented by a tuple
    /// containing the site name, instruction file, and output
    /// directory.
    /// </returns>
    IEnumerable<(string SiteName, string InstructionFile, string OutputDirectory)> FindSiteLevelRuns(string repoPath);
}
