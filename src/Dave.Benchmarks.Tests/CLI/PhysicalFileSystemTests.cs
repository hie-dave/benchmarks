using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Tests.Helpers;

namespace Dave.Benchmarks.Tests.CLI;

public class PhysicalFileSystemTests
{
    [Fact]
    public void EnumerateFiles_TopDirectoryOnly_DoesNotIncludeNestedFiles()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string rootOut = Path.Combine(temp.AbsolutePath, "root.out");
        string subDir = Path.Combine(temp.AbsolutePath, "nested");
        Directory.CreateDirectory(subDir);
        string nestedOut = Path.Combine(subDir, "nested.out");

        File.WriteAllText(rootOut, "a");
        File.WriteAllText(nestedOut, "b");

        IFileSystem fs = new PhysicalFileSystem();
        var files = fs.EnumerateFiles(temp.AbsolutePath, "*.out", SearchOption.TopDirectoryOnly).ToArray();

        Assert.Single(files);
        Assert.Equal(rootOut, files[0]);
    }

    [Fact]
    public void EnumerateFiles_AllDirectories_IncludesNestedFiles()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string rootOut = Path.Combine(temp.AbsolutePath, "root.out");
        string subDir = Path.Combine(temp.AbsolutePath, "nested");
        Directory.CreateDirectory(subDir);
        string nestedOut = Path.Combine(subDir, "nested.out");

        File.WriteAllText(rootOut, "a");
        File.WriteAllText(nestedOut, "b");

        IFileSystem fs = new PhysicalFileSystem();
        var files = fs.EnumerateFiles(temp.AbsolutePath, "*.out", SearchOption.AllDirectories).OrderBy(x => x).ToArray();

        Assert.Equal(2, files.Length);
        Assert.Equal(new[] { nestedOut, rootOut }.OrderBy(x => x), files);
    }

    [Fact]
    public void EnumerateFiles_RespectsSearchPattern()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string outFile = Path.Combine(temp.AbsolutePath, "a.out");
        string txtFile = Path.Combine(temp.AbsolutePath, "b.txt");
        File.WriteAllText(outFile, "a");
        File.WriteAllText(txtFile, "b");

        IFileSystem fs = new PhysicalFileSystem();
        var files = fs.EnumerateFiles(temp.AbsolutePath, "*.out", SearchOption.TopDirectoryOnly).ToArray();

        Assert.Single(files);
        Assert.Equal(outFile, files[0]);
    }

    [Fact]
    public void GetLastWriteTime_ExistingFile_MatchesBcl()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string file = Path.Combine(temp.AbsolutePath, "a.out");
        File.WriteAllText(file, "a");
        DateTime expected = new DateTime(2025, 01, 01, 12, 30, 00);
        File.SetLastWriteTime(file, expected);

        IFileSystem fs = new PhysicalFileSystem();
        DateTime actual = fs.GetLastWriteTime(file);

        Assert.Equal(File.GetLastWriteTime(file), actual);
    }

    [Fact]
    public void GetLastWriteTime_MissingPath_UsesBclBehavior()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string missing = Path.Combine(temp.AbsolutePath, "missing.out");

        IFileSystem fs = new PhysicalFileSystem();
        DateTime actual = fs.GetLastWriteTime(missing);

        Assert.Equal(File.GetLastWriteTime(missing), actual);
    }
}
