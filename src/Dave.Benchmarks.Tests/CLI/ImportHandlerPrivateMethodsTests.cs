using System.Reflection;
using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Services;
using Dave.Benchmarks.Tests.Helpers;
using LpjGuess.Core.Helpers;
using LpjGuess.Core.Parsers;
using LpjGuess.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Dave.Benchmarks.Tests.CLI;

public class ImportHandlerPrivateMethodsTests
{
    private class TestResolver : IOutputFileTypeResolver
    {
        public string GetFileType(string filename) => "dummy";
        public void BuildLookupTable(LpjGuess.Core.Parsers.InstructionFileParser parser) { }
    }

    private ImportHandler CreateHandler()
    {
        ILogger<ModelOutputParser> l = NullLogger<ModelOutputParser>.Instance;
        var parser = new ModelOutputParser(l, new TestResolver());

        var git = new GitService(NullLogger<GitService>.Instance);
        var api = new Mock<IApiClient>();
        var resolver = new TestResolver();
        var gridlist = new GridlistParser(NullLogger<GridlistParser>.Instance);
        var instrLogger = NullLogger<InstructionFileHelper>.Instance;

        return new ImportHandler(NullLogger<ImportHandler>.Instance, parser,
                                 git, api.Object, resolver, gridlist,
                                 instrLogger);
    }

    [Fact]
    public async Task GetMostRecentWriteTime_ReturnsLatest()
    {
        var handler = CreateHandler();
        using TempDirectory temp = TempDirectory.Create(GetType().Name);

        string f1 = Path.Combine(temp.AbsolutePath, "a.out");
        string f2 = Path.Combine(temp.AbsolutePath, "b.out");
        await File.WriteAllTextAsync(f1, "x");
        await File.WriteAllTextAsync(f2, "y");

        DateTime now = DateTime.Now;
        File.SetLastWriteTime(f1, now.AddSeconds(-10));
        File.SetLastWriteTime(f2, now);

        var mi = typeof(ImportHandler).GetMethod("GetMostRecentWriteTime", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (DateTime)mi.Invoke(handler, new object[] { new string[] { f1, f2 } });
        Assert.Equal(File.GetLastWriteTime(f2), result);
    }

    [Fact]
    public async Task IsStaleFile_DetectsStale()
    {
        var handler = CreateHandler();
        using TempDirectory temp = TempDirectory.Create(GetType().Name);

        string f1 = Path.Combine(temp.AbsolutePath, "a.out");
        await File.WriteAllTextAsync(f1, "x");
        DateTime now = DateTime.Now;
        File.SetLastWriteTime(f1, now.AddSeconds(-10));

        var getMostRecent = typeof(ImportHandler).GetMethod("GetMostRecentWriteTime", BindingFlags.NonPublic | BindingFlags.Instance);
        var isStale = typeof(ImportHandler).GetMethod("IsStaleFile", BindingFlags.NonPublic | BindingFlags.Instance);

        DateTime mostRecent = now; // simulate most recent time
        bool stale = (bool)isStale.Invoke(handler, new object[] { f1, mostRecent })!;
        Assert.True(stale);
    }
}
