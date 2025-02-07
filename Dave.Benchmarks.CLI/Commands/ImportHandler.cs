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
    // Site-level runs are point-scale by definition.
    private const string siteLevelSpatialResolution = "Point";

    /// <summary>
    /// API endpoint used to upload data to a dataset.
    /// </summary>
    private const string addEndpoint = "api/predictions/add";

    /// <summary>
    /// API endpoint used to create a dataset.
    /// </summary>
    private const string createEndpoint = "api/predictions/create";

    private readonly ILogger<ImportHandler> _logger;
    private readonly ModelOutputParser _parser;
    private readonly GitService _gitService;
    private readonly InstructionFileParser _instructionParser;
    private readonly HttpClient _httpClient;
    private readonly IOutputFileTypeResolver _resolver;

    /// <summary>
    /// Sites with output files written more than this number of seconds before
    /// the most recent write time of any site-level run are considered stale.
    /// </summary>
    private const int staleSiteThresholdSeconds = 300;

    /// <summary>
    /// Output files written more than this number of seconds before the 
    /// newest file are considered stale.
    /// </summary>
    private const double staleFileThresholdSeconds = 5.0;

    public ImportHandler(
        ILogger<ImportHandler> logger,
        ModelOutputParser parser,
        GitService gitService,
        InstructionFileParser instructionParser,
        HttpClient httpClient,
        IOutputFileTypeResolver resolver)
    {
        _logger = logger;
        _parser = parser;
        _gitService = gitService;
        _instructionParser = instructionParser;
        _httpClient = httpClient;
        _resolver = resolver;

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
        
        return age.TotalSeconds > staleFileThresholdSeconds;
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

    private string[] EnumerateOutputFiles(string directory)
    {
        return Directory.GetFiles(directory, "*.out");
    }

    private async Task<int> CreateDataset(
        string name,
        string description,
        string parameters,
        GitService.RepositoryInfo repoInfo,
        string climateDataset,
        string spatialResolution,
        string temporalResolution)
    {
        var request = new CreateDatasetRequest
        {
            Name = name,
            Description = description,
            ModelVersion = repoInfo.CommitHash,
            ClimateDataset = climateDataset,
            SpatialResolution = spatialResolution,
            TemporalResolution = temporalResolution,
            Parameters = parameters,
            CodePatches = repoInfo.Patches
        };

        var response = await _httpClient.PostAsJsonAsync(createEndpoint, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateDatasetResponse>();
        if (result == null)
            throw new InvalidOperationException("Server returned an empty response when creating dataset");
            
        return result.Id;
    }

    private async Task ImportOutputFile(int datasetId, string outputFile)
    {
        _logger.LogDebug("Importing {File}", outputFile);

        // Parse output file
        Quantity quantity = await _parser.ParseOutputFileAsync(outputFile);

        var request = new ImportModelPredictionRequest
        {
            DatasetId = datasetId,
            Quantity = quantity
        };

        var response = await _httpClient.PostAsJsonAsync(addEndpoint, request);
        response.EnsureSuccessStatusCode();
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
        GitService.RepositoryInfo repoInfo = _gitService.GetRepositoryInfo(options.RepoPath);

        // Parse instruction file
        string parameters = await _instructionParser.ParseInstructionFileAsync(options.InstructionFile);

        // Get all output files and find most recent write time
        string[] outputFiles = Directory.GetFiles(options.OutputDir, "*.out");
        if (outputFiles.Length == 0)
            return;

        DateTime mostRecentWriteTime = GetMostRecentWriteTime(outputFiles);

        // Create dataset
        int datasetId = await CreateDataset(
            options.Name,
            options.Description,
            parameters,
            repoInfo,
            options.ClimateDataset,
            options.SpatialResolution,
            options.TemporalResolution);

        // Process each output file, skipping stale ones
        foreach (string outputFile in outputFiles)
        {
            if (IsStaleFile(outputFile, mostRecentWriteTime))
            {
                EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                continue;
            }

            await ImportOutputFile(datasetId, outputFile);
        }
    }

    public async Task HandleSiteImport(SiteOptions options)
    {
        using var _ = _logger.BeginScope("importer");

        // Find all site-level runs
        IEnumerable<(string, string, string)> runs = _gitService.FindSiteLevelRuns(options.RepoPath).ToList();
        if (!runs.Any())
            throw new InvalidOperationException("No site-level runs found in repository");

        // Get repository info
        GitService.RepositoryInfo repoInfo = _gitService.GetRepositoryInfo(options.RepoPath);

        // Get time stamp of most recently-written output file.
        IEnumerable<string> allFiles = runs.SelectMany(r => EnumerateOutputFiles(r.Item3));
        DateTime globalTimestamp = GetMostRecentWriteTime(allFiles.ToArray());

        foreach ((string siteName, string instructionFile, string outputDir) in runs)
        {
            // Parse instruction file
            string parameters = await _instructionParser.ParseInstructionFileAsync(instructionFile);
            
            // Build the lookup table for this site's output files
            _resolver.BuildLookupTable(_instructionParser);

            // Get all output files and find most recent write time
            string[] outputFiles = EnumerateOutputFiles(outputDir);
            if (!outputFiles.Any())
            {
                // GetMostRecentWriteTime() will throw on empty collections.
                _logger.LogWarning("Site {siteName} has no output files", siteName);
                continue;
            }

            // Get most recent write time for this site.
            DateTime mostRecentWriteTime = GetMostRecentWriteTime(outputFiles);

            if ((globalTimestamp - mostRecentWriteTime).TotalSeconds > staleSiteThresholdSeconds)
            {
                _logger.LogWarning("Site {siteName} is stale (age: {age})", siteName, TimeUtils.FormatTimeSpan(globalTimestamp - mostRecentWriteTime));
                continue;
            }

            // Create dataset
            int datasetId = await CreateDataset(
                options.Name,
                options.Description,
                parameters,
                repoInfo,
                options.ClimateDataset,
                siteLevelSpatialResolution,
                options.TemporalResolution);

            // Process each output file, skipping stale ones
            foreach (string outputFile in outputFiles)
            {
                if (IsStaleFile(outputFile, mostRecentWriteTime))
                {
                    EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                    continue;
                }

                string variableName = Path.GetFileNameWithoutExtension(outputFile);
                await ImportOutputFile(datasetId, outputFile);
            }
        }
    }
}
