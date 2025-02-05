using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dave.Benchmarks.Core.Services;
using Xunit;

namespace Dave.Benchmarks.Tests.Services;

public class ModelOutputParserTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly ModelOutputParser _parser;

    public ModelOutputParserTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "output_tests");
        _parser = new ModelOutputParser();
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
    public async Task ParseOutputFile_ValidFormat_Success()
    {
        // Arrange
        var content = @"Longitude Latitude Year Day LAI NPP
151.25 -33.75 2000 1 2.5 10.2
151.25 -33.75 2000 2 2.6 10.5
151.50 -33.75 2000 1 1.8 8.4";
        var filePath = Path.Combine(_testDir, "test.out");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var (variables, dataPoints) = await _parser.ParseOutputFileAsync(filePath, datasetId: 1);

        // Assert
        Assert.Equal(2, variables.Count); // LAI and NPP
        Assert.Equal(6, dataPoints.Count); // 2 variables * 3 data points
        
        Assert.Contains(variables, v => v.Name == "LAI");
        Assert.Contains(variables, v => v.Name == "NPP");

        var laiPoints = dataPoints.Where(d => d.VariableId == variables.First(v => v.Name == "LAI").Id).ToList();
        Assert.Equal(3, laiPoints.Count);
        Assert.Contains(laiPoints, p => Math.Abs(p.Value - 2.5) < 0.001);
    }

    [Fact]
    public async Task ParseOutputFile_InvalidHeader_ThrowsException()
    {
        // Arrange
        var content = @"Invalid Header Format
151.25 -33.75 2000 1 2.5 10.2";
        var filePath = Path.Combine(_testDir, "invalid.out");
        await File.WriteAllTextAsync(filePath, content);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => _parser.ParseOutputFileAsync(filePath, datasetId: 1));
    }

    [Fact]
    public async Task ParseOutputFile_InvalidDataRow_ThrowsException()
    {
        // Arrange
        var content = @"Longitude Latitude Year Day LAI NPP
151.25 -33.75 2000 1 invalid 10.2";
        var filePath = Path.Combine(_testDir, "invalid_data.out");
        await File.WriteAllTextAsync(filePath, content);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => _parser.ParseOutputFileAsync(filePath, datasetId: 1));
    }

    [Fact]
    public async Task ParseOutputFile_EmptyFile_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "empty.out");
        await File.WriteAllTextAsync(filePath, string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => _parser.ParseOutputFileAsync(filePath, datasetId: 1));
    }
}
