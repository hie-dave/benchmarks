using CommandLine;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.Tests.Helpers;

namespace Dave.Benchmarks.Tests.CLI;

public class OptionsTests
{
    [Fact]
    public void Parse_GriddedOptions_DefaultsAreSet()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string[] args = [
            "gridded",
            "-o", Path.Combine(temp.AbsolutePath, "out"),
            "-i", Path.Combine(temp.AbsolutePath, "file.ins"),
            "-s", "0.5",
            "--simulation-id", "sim1",
            "-r", Path.Combine(temp.AbsolutePath, "repo"),
            "-n", "name",
            "-d", "desc",
            "-c", "climate",
            "--temporal-resolution", "3-hourly"
        ];

        var parse = Parser.Default.ParseArguments<GriddedOptions, SiteOptions>(args);
        OptionsBase found = parse.MapResult(
            (GriddedOptions opts) => opts as OptionsBase,
            (SiteOptions s) => throw new Exception("Expected GriddedOptions, got SiteOptions"),
            errs => throw new Exception("Failed to parse options: " + string.Join(", ", errs))
        );

        Assert.NotNull(found);
        var g = (GriddedOptions)found!;
        Assert.Equal("lpjguess_dave", g.BaselineChannel);
        Assert.False(g.DryRun);
    }

    [Fact]
    public void Parse_SiteOptions_ParsesVerb()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string[] args = [
            "site",
            "-r", Path.Combine(temp.AbsolutePath, "repo"),
            "-n", "name",
            "-d", "desc",
            "-c", "climate",
            "--temporal-resolution", "3-hourly"
        ];

        var parse = Parser.Default.ParseArguments<GriddedOptions, SiteOptions>(args);
        bool res = parse.MapResult(
            (GriddedOptions g) => false,
            (SiteOptions s) => true,
            errs => false
        );
        Assert.True(res);
    }
}
