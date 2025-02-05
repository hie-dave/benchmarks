using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models;
using Dave.Benchmarks.Core.Services;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Commands;

public class ImportHandler
{
    private readonly ILogger<ImportHandler> _logger;
    private readonly BenchmarksDbContext _dbContext;
    private readonly ModelOutputParser _parser;
    private readonly GitService _gitService;
    private readonly InstructionFileParser _instructionParser;

    public ImportHandler(
        ILogger<ImportHandler> logger,
        BenchmarksDbContext dbContext,
        ModelOutputParser parser,
        GitService gitService,
        InstructionFileParser instructionParser)
    {
        _logger = logger;
        _dbContext = dbContext;
        _parser = parser;
        _gitService = gitService;
        _instructionParser = instructionParser;
    }

    public async Task HandleGriddedImport(GriddedOptions options)
    {
        // Get repository info
        var repoInfo = _gitService.GetRepositoryInfo(options.InstructionFile, options.RepoPath);

        // Parse instruction file
        var parameters = await _instructionParser.ParseInstructionFileAsync(options.InstructionFile);

        // Process each output file
        foreach (var outputFile in Directory.GetFiles(options.OutputDir, "*.out"))
        {
            var variableName = Path.GetFileNameWithoutExtension(outputFile);
            await ImportOutputFile(
                outputFile,
                variableName,
                string.Format(options.Description, variableName),
                parameters,
                repoInfo,
                options.ClimateDataset,
                options.SpatialResolution,
                options.TemporalResolution);
        }
    }

    public async Task HandleSiteImport(SiteOptions options)
    {
        // Find all site-level runs
        var runs = _gitService.FindSiteLevelRuns(options.RepoPath).ToList();
        if (!runs.Any())
        {
            throw new InvalidOperationException("No site-level runs found in repository");
        }

        foreach (var (siteName, instructionFile, outputDir) in runs)
        {
            // Get repository info
            var repoInfo = _gitService.GetRepositoryInfo(instructionFile, options.RepoPath);

            // Parse instruction file
            var parameters = await _instructionParser.ParseInstructionFileAsync(instructionFile);

            // Process each output file
            foreach (var outputFile in Directory.GetFiles(outputDir, "*.out"))
            {
                var variableName = Path.GetFileNameWithoutExtension(outputFile);
                await ImportOutputFile(
                    outputFile,
                    variableName,
                    options.Description,
                    parameters,
                    repoInfo,
                    options.ClimateDataset,
                    "Point",  // Site-level runs are always point-scale
                    "Daily"); // Site-level runs are always daily
            }
        }
    }

    private async Task ImportOutputFile(
        string outputFile,
        string name,
        string description,
        string parameters,
        GitService.RepositoryInfo repoInfo,
        string climateDataset,
        string spatialResolution,
        string temporalResolution)
    {
        _logger.LogInformation("Importing {File}", outputFile);

        var dataset = new ModelPredictionDataset
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            ModelVersion = repoInfo.CommitHash,
            ClimateDataset = climateDataset,
            SpatialResolution = spatialResolution,
            TemporalResolution = temporalResolution,
            CodePatches = repoInfo.Patches
        };

        dataset.SetParameters(parameters);

        // Save dataset to get an ID
        _dbContext.ModelPredictions.Add(dataset);
        await _dbContext.SaveChangesAsync();

        // Parse output file
        var (variables, dataPoints) = await _parser.ParseOutputFileAsync(outputFile, dataset.Id);

        // Save variables to get IDs
        await _dbContext.Variables.AddRangeAsync(variables);
        await _dbContext.SaveChangesAsync();

        // Update variable IDs in data points
        foreach (var dataPoint in dataPoints)
        {
            dataPoint.VariableId = variables[dataPoint.VariableId].Id;
        }

        // Save data points in batches
        const int batchSize = 10000;
        for (int i = 0; i < dataPoints.Count; i += batchSize)
        {
            var batch = dataPoints.Skip(i).Take(batchSize);
            await _dbContext.DataPoints.AddRangeAsync(batch);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} data points", i + batch.Count());
        }
    }
}
