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
using Dave.Benchmarks.Core.Utils;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Commands;

/// <summary>
/// Handles importing model predictions from output files to the database.
/// </summary>
public class ImportHandler
{
    private const string endpoint = "api/predictions/import";

    private readonly ILogger<ImportHandler> _logger;
    private readonly ModelOutputParser _parser;
    private readonly GitService _gitService;
    private readonly InstructionFileParser _instructionParser;
    private readonly HttpClient _httpClient;

    private const double StaleFileThresholdSeconds = 5.0; // Files more than 5 seconds older than the newest file are considered stale

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

        if (_httpClient.BaseAddress == null)
            throw new InvalidOperationException("ImportHandler: Base address is null");
    }

    /// <summary>
    /// Gets the most recent write time from a set of output files
    /// </summary>
    /// <param name="outputFiles">The files to check.</param>
    /// <returns>The most recent write time.</returns>
    private DateTime GetMostRecentWriteTime(string[] outputFiles)
    {
        return outputFiles
            .Select(f => new FileInfo(f))
            .Max(f => f.LastWriteTime);
    }

    /// <summary>
    /// Checks if a file is stale by comparing its write time to the most recent write time
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="mostRecentWriteTime">The most recent write time.</param>
    /// <returns>True if the file is stale, false otherwise.</returns>
    private bool IsStaleFile(string filePath, DateTime mostRecentWriteTime)
    {
        FileInfo fileInfo = new(filePath);
        TimeSpan age = mostRecentWriteTime - fileInfo.LastWriteTime;
        
        return age.TotalSeconds > StaleFileThresholdSeconds;
    }

    /// <summary>
    /// Emits a warning for a stale file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="mostRecentWriteTime">The most recent write time.</param>
    private void EmitStaleFileWarning(string filePath, DateTime mostRecentWriteTime)
    {
        FileInfo fileInfo = new(filePath);
        TimeSpan age = mostRecentWriteTime - fileInfo.LastWriteTime;
        
        _logger.LogWarning("Skipping stale output file (age: {age}): {filePath}", 
            TimeUtils.FormatTimeSpan(age),
            filePath);
    }

    /// <summary>
    /// Import all output files from a gridded run and upload them to the DB.
    /// </summary>
    /// <param name="options">The options for the gridded import.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleGriddedImport(GriddedOptions options)
    {
        // Get repository info
        using var _ = _logger.BeginScope("importer");
        GitService.RepositoryInfo repoInfo = _gitService.GetRepositoryInfo(options.InstructionFile, options.RepoPath);

        // Parse instruction file
        string parameters = await _instructionParser.ParseInstructionFileAsync(options.InstructionFile);

        // Get all output files and find most recent write time
        string[] outputFiles = Directory.GetFiles(options.OutputDir, "*.out");
        if (outputFiles.Length == 0)
            return;

        DateTime mostRecentWriteTime = GetMostRecentWriteTime(outputFiles);

        // Process each output file, skipping stale ones
        foreach (string outputFile in outputFiles)
        {
            if (IsStaleFile(outputFile, mostRecentWriteTime))
            {
                EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                continue;
            }

            string variableName = Path.GetFileNameWithoutExtension(outputFile);
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
        using var _ = _logger.BeginScope("importer");

        // Find all site-level runs
        IEnumerable<(string, string, string)> runs = _gitService.FindSiteLevelRuns(options.RepoPath).ToList();
        if (!runs.Any())
            throw new InvalidOperationException("No site-level runs found in repository");

        foreach ((string siteName, string instructionFile, string outputDir) in runs)
        {
            // Get repository info
            GitService.RepositoryInfo repoInfo = _gitService.GetRepositoryInfo(instructionFile, options.RepoPath);

            // Parse instruction file
            string parameters = await _instructionParser.ParseInstructionFileAsync(instructionFile);

            // Get all output files and find most recent write time
            string[] outputFiles = Directory.GetFiles(outputDir, "*.out");
            if (outputFiles.Length == 0)
                continue;

            DateTime mostRecentWriteTime = GetMostRecentWriteTime(outputFiles);

            // Process each output file, skipping stale ones
            foreach (string outputFile in outputFiles)
            {
                if (IsStaleFile(outputFile, mostRecentWriteTime))
                {
                    EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                    continue;
                }


                string variableName = Path.GetFileNameWithoutExtension(outputFile);
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

    /// <summary>
    /// Imports a single output file and uploads it to the DB.
    /// </summary>
    /// <param name="outputFile">Path to the output file.</param>
    /// <param name="name">Name of the quantity.</param>
    /// <param name="description">Description of the quantity.</param>
    /// <param name="parameters">Parameters used to run the model.</param>
    /// <param name="repoInfo">Information about the repository.</param>
    /// <param name="climateDataset">Climate dataset used in the run.</param>
    /// <param name="spatialResolution">Spatial resolution of the run.</param>
    /// <param name="temporalResolution">Temporal resolution of the run.</param>
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

        ImportModelPredictionRequest request = new()
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

        _logger.LogDebug("Sending request to: {Uri}", _httpClient.BaseAddress != null 
            ? new Uri(_httpClient.BaseAddress, endpoint).ToString()
            : endpoint);

        return;
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(endpoint, request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to import predictions. Status code: {StatusCode}, Reason: {ReasonPhrase}", 
                response.StatusCode, 
                response.ReasonPhrase);
        }

        response.EnsureSuccessStatusCode();
    }
}
