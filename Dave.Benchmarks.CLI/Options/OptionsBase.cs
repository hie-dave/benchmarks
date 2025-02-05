using CommandLine;
using CommandLine.Text;

namespace Dave.Benchmarks.CLI.Options;

public class OptionsBase
{
    [Option('r', "repo-path", Required = true, HelpText = "Path to the git repository")]
    public string RepoPath { get; set; } = string.Empty;

    [Option('d', "description", Required = true, HelpText = "Description of Simulations")]
    public string Description { get; set; } = string.Empty;

    [Option('c', "climate-dataset", Required = true, HelpText = "Name/version of the climate dataset used")]
    public string ClimateDataset { get; set; } = string.Empty;
}
