using CommandLine;
using CommandLine.Text;

namespace Dave.Benchmarks.CLI.Options;

public class OptionsBase
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

    [Option("dry-run", Required = false, Default = false, HelpText = "Run without making any requests to the web server")]
    public bool DryRun { get; set; } = false;
}
