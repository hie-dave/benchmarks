using CommandLine;

namespace Dave.Benchmarks.CLI.Options;

[Verb("evaluate", HelpText = "Start evaluation of an imported prediction dataset")]
public class EvaluateOptions
{
    [Option("submission-id", Required = true, HelpText = "Completed benchmark submission ID")]
    public int SubmissionId { get; set; }

    [Option("wait", Default = false, HelpText = "Poll until evaluation completes and fail if the gate fails")]
    public bool Wait { get; set; }

    [Option("timeout-seconds", Default = 1800, HelpText = "Maximum wait time when --wait is used")]
    public int TimeoutSeconds { get; set; } = 1800;

    [Option("poll-interval-seconds", Default = 5, HelpText = "Polling interval when --wait is used")]
    public int PollIntervalSeconds { get; set; } = 5;
}
