namespace Dave.Benchmarks.Core.Services;

public class RepositoryInfo
{
    public string CommitHash { get; set; } = string.Empty;
    public byte[] Patches { get; set; } = Array.Empty<byte>();
    public string RepositoryPath { get; set; } = string.Empty;
    public bool HasUncommittedChanges { get; set; }
}
