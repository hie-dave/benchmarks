using System.IO;
using System.Threading.Tasks;
using Dave.Benchmarks.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dave.Benchmarks.Tests.Services;

public class InstructionFileParserTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly InstructionFileParser _parser;

    public InstructionFileParserTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "instruction_tests");
        var logger = new Mock<ILogger<InstructionFileParser>>();
        _parser = new InstructionFileParser(logger.Object);
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_testDir, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ParseInstructionFile_SingleFile_Success()
    {
        // Arrange
        var content = @"! This is a comment
param1 = value1
! Another comment
param2 = value2";
        var filePath = Path.Combine(_testDir, "test.ins");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _parser.ParseInstructionFileAsync(filePath);

        // Assert
        Assert.Contains("! This is a comment", result);
        Assert.Contains("param1 = value1", result);
        Assert.Contains("param2 = value2", result);
    }

    [Fact]
    public async Task ParseInstructionFile_WithImports_Success()
    {
        // Arrange
        var mainContent = @"! Main file
param1 = value1
import ""sub1.ins""
param2 = value2";
        var sub1Content = @"! Sub file 1
sub1_param = sub1_value
import ""sub2.ins""";
        var sub2Content = @"! Sub file 2
sub2_param = sub2_value";

        var mainPath = Path.Combine(_testDir, "main.ins");
        var sub1Path = Path.Combine(_testDir, "sub1.ins");
        var sub2Path = Path.Combine(_testDir, "sub2.ins");

        await File.WriteAllTextAsync(mainPath, mainContent);
        await File.WriteAllTextAsync(sub1Path, sub1Content);
        await File.WriteAllTextAsync(sub2Path, sub2Content);

        // Act
        var result = await _parser.ParseInstructionFileAsync(mainPath);

        // Assert
        Assert.Contains("param1 = value1", result);
        Assert.Contains("sub1_param = sub1_value", result);
        Assert.Contains("sub2_param = sub2_value", result);
        Assert.Contains("param2 = value2", result);
    }

    [Fact]
    public async Task ParseInstructionFile_CircularImport_HandledGracefully()
    {
        // Arrange
        var file1Content = @"param1 = value1
import ""file2.ins""";
        var file2Content = @"param2 = value2
import ""file1.ins""";

        var file1Path = Path.Combine(_testDir, "file1.ins");
        var file2Path = Path.Combine(_testDir, "file2.ins");

        await File.WriteAllTextAsync(file1Path, file1Content);
        await File.WriteAllTextAsync(file2Path, file2Content);

        // Act
        var result = await _parser.ParseInstructionFileAsync(file1Path);

        // Assert
        Assert.Contains("param1 = value1", result);
        Assert.Contains("param2 = value2", result);
        // Second occurrence of param1 should not appear due to circular import detection
        Assert.Single(result.Split("param1 = value1"), s => !string.IsNullOrEmpty(s));
    }

    [Fact]
    public async Task ParseInstructionFile_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.ins");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _parser.ParseInstructionFileAsync(nonExistentPath));
    }
}
