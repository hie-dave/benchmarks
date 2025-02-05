using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Commands;

public class ImportHandler
{
    private readonly ILogger<ImportHandler> _logger;
    private readonly ModelOutputParser _parser;
    private readonly GitService _gitService;
    private readonly InstructionFileParser _instructionParser;
    private readonly HttpClient _httpClient;

    public ImportHandler(
        ILogger<ImportHandler> logger,
        ModelOutputParser parser,
        GitService gitService,
        InstructionFileParser instructionParser,
        HttpClient httpClient)
    {
        _logger = logger;
        _parser = parser;
        _gitService = gitService;
        _instructionParser = instructionParser;
        _httpClient = httpClient;
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

        // Parse output file
        Quantity quantity = await _parser.ParseOutputFileAsync(outputFile);

        var request = new ImportModelPredictionRequest
        {
            Name = name,
            Description = description,
            ModelVersion = repoInfo.CommitHash,
            ClimateDataset = climateDataset,
            SpatialResolution = spatialResolution,
            TemporalResolution = temporalResolution,
            Parameters = parameters,
            CodePatches = repoInfo.Patches,
            Quantity = quantity
        };

        var response = await _httpClient.PostAsJsonAsync("api/predictions/import", request);
        response.EnsureSuccessStatusCode();
    }
}
