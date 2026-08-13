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

        var parse = Parser.Default.ParseArguments<GriddedOptions, SiteOptions, EvaluateOptions, BenchmarkOptions>(args);
        OptionsBase found = parse.MapResult(
            (GriddedOptions opts) => opts as OptionsBase,
            (SiteOptions s) => throw new Exception("Expected GriddedOptions, got SiteOptions"),
            (EvaluateOptions e) => throw new Exception("Expected GriddedOptions, got EvaluateOptions"),
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

        var parse = Parser.Default.ParseArguments<GriddedOptions, SiteOptions, EvaluateOptions, BenchmarkOptions>(args);
        bool res = parse.MapResult(
            (GriddedOptions g) => false,
            (SiteOptions s) => true,
            (EvaluateOptions e) => false,
            errs => false
        );
        Assert.True(res);
    }

    [Fact]
    public void Parse_EvaluateOptions_ParsesWaitAndDefaults()
    {
        string[] args = [
            "evaluate",
            "--submission-id", "42",
            "--wait"
        ];

        var parse = Parser.Default.ParseArguments<GriddedOptions, SiteOptions, EvaluateOptions, BenchmarkOptions>(args);
        EvaluateOptions? found = null;
        parse.WithParsed<EvaluateOptions>(options => found = options);

        Assert.NotNull(found);
        Assert.Equal(42, found.SubmissionId);
        Assert.True(found.Wait);
        Assert.Equal(1800, found.TimeoutSeconds);
        Assert.Equal(5, found.PollIntervalSeconds);
    }
}
