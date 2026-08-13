using CommandLine;

namespace Dave.Benchmarks.CLI.Options;

public abstract class ImportOptionsBase : OptionsBase
{
    [Option('r', "repo-path", Required = true, HelpText = "Path to the git repository")]
    public string RepoPath { get; set; } = string.Empty;

    [Option('n', "name", Required = true, HelpText = "Name of the dataset")]
    public string Name { get; set; } = string.Empty;

    [Option('d', "description", Required = true, HelpText = "Description of the dataset")]
    public string Description { get; set; } = string.Empty;

    [Option('c', "climate-dataset", Required = true, HelpText = "Name/version of the climate dataset used")]
    public string ClimateDataset { get; set; } = string.Empty;

    [Option("temporal-resolution", Required = true, HelpText = "Temporal resolution of the dataset")]
    public string TemporalResolution { get; set; } = string.Empty;

    [Option("baseline-channel", Required = false, Default = "lpjguess_dave", HelpText = "Baseline channel used for evaluation/baseline scope")]
    public string BaselineChannel { get; set; } = "lpjguess_dave";

    [Option("submission-id", Required = false, HelpText = "Benchmark submission receiving imported datasets")]
    public int? SubmissionId { get; set; }

    [Option("cleanup-on-failure", Default = false, HelpText = "Delete the partially imported group when import fails")]
    public bool CleanupOnFailure { get; set; }
}
