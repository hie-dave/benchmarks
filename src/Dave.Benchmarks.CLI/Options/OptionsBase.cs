using CommandLine;

namespace Dave.Benchmarks.CLI.Options;

public abstract class OptionsBase
{
    [Option("dry-run", Required = false, Default = false, HelpText = "Run without making any requests to the web server")]
    public bool DryRun { get; set; } = false;

}
