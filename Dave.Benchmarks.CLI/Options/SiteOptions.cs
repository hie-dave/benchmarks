using CommandLine;
using CommandLine.Text;

namespace Dave.Benchmarks.CLI.Options;

[Verb("site", HelpText = "Import site-level model output files")]
public class SiteOptions : OptionsBase
{
    [Usage(ApplicationAlias = "dave-benchmarks")]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example("Import site-level model output", new SiteOptions
            {
                RepoPath = "/path/to/repo",
                Name = "DAVE",
                Description = "DAVE site-level runs",
                ClimateDataset = "Ozflux",
                TemporalResolution = "3-hourly",
            });
        }
    }
}
