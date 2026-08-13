using CommandLine;

namespace Dave.Benchmarks.CLI.Options;

[Verb("benchmark", HelpText = "Import a site benchmark submission and evaluate it")]
public class BenchmarkOptions : ImportOptionsBase
{
    [Option("merge-request-id", Required = true)] public string MergeRequestId { get; set; } = string.Empty;
    [Option("pipeline-id", Required = true)] public string PipelineId { get; set; } = string.Empty;
    [Option("commit-sha", Required = true)] public string CommitSha { get; set; } = string.Empty;
    [Option("commit-message")] public string? CommitMessage { get; set; }
    [Option("source-branch", Required = true)] public string SourceBranch { get; set; } = string.Empty;
    [Option("target-branch", Required = true)] public string TargetBranch { get; set; } = string.Empty;
    [Option("benchmark-name", Default = "site-benchmarks")] public string BenchmarkName { get; set; } = "site-benchmarks";
    [Option("timeout-seconds", Default = 1800)] public int TimeoutSeconds { get; set; } = 1800;
    [Option("poll-interval-seconds", Default = 5)] public int PollIntervalSeconds { get; set; } = 5;
}
