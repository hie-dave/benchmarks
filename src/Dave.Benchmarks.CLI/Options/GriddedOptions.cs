using CommandLine;
using CommandLine.Text;

namespace Dave.Benchmarks.CLI.Options;

[Verb("gridded", HelpText = "Import gridded model output files")]
public class GriddedOptions : OptionsBase
{
    [Option('o', "output-dir", Required = true, HelpText = "Directory containing model outputs")]
    public string OutputDir { get; set; } = string.Empty;

    [Option('i', "instruction-file", Required = true, HelpText = "Path to the instruction file used for the run")]
    public string InstructionFile { get; set; } = string.Empty;

    [Option('s', "spatial-resolution", Required = true, HelpText = "Spatial resolution of the runs")]
    public string SpatialResolution { get; set; } = string.Empty;

    [Usage(ApplicationAlias = "dave-benchmarks")]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example("Import gridded model output", new GriddedOptions
            {
                OutputDir = "/path/to/outputs",
                RepoPath = "/path/to/repo",
                Description = "Simulations of SE Australia",
                InstructionFile = "/path/to/file.ins",
                ClimateDataset = "BARPA historical-ssp370",
                SpatialResolution = "0.5°",
                TemporalResolution = "3-Hourly"
            });
        }
    }
}
