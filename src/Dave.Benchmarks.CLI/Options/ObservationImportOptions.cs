using CommandLine;

namespace Dave.Benchmarks.CLI.Options;

[Verb("observations", HelpText = "Import a versioned observation release from a YAML manifest")]
public class ObservationImportOptions : OptionsBase
{
    [Option("manifest", Required = true, HelpText = "Path to the observation YAML manifest")]
    public string Manifest { get; set; } = string.Empty;

    [Option("activate", Default = false, HelpText = "Activate the completed release after import")]
    public bool Activate { get; set; }
}
