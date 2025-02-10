using System.Net.Http.Json;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;
using Dave.Benchmarks.Core.Utilities;
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
    private const string addEndpoint = "api/predictions/{0}/add";

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
    /// Tracks individual-PFT mappings for the current dataset being imported.
    /// </summary>
    private IReadOnlyDictionary<int, string>? indivMappings;

    /// <summary>
    /// The output file from which indivMappings was created.
    /// </summary>
    private string? indivMappingsFile;

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

    private void ValidateIndivPftMappings(Quantity quantity, string fileName)
    {
        if (quantity.Level != AggregationLevel.Individual)
        {
            if (quantity.IndividualPfts != null)
                ExceptionHelper.Throw<InvalidDataException>(_logger, $"File {fileName} is not an indiv-level output, and should therefore not contain indiv-pft mappings");
            return;
        }

        if (indivMappings == null)
        {
            // First individual-level file, store the mapping and return.
            indivMappings = quantity.IndividualPfts;
            indivMappingsFile = fileName;
            return;
        }

        if (quantity.IndividualPfts == null)
            ExceptionHelper.Throw<InvalidDataException>(_logger, $"File {fileName} is an indiv-level output, but does not contain indiv-pft mappings");

        // Compare with existing mapping
        foreach ((int indivId, string pftName) in quantity.IndividualPfts!)
        {
            if (!indivMappings.TryGetValue(indivId, out var existingPft))
            {
                throw new InvalidDataException(
                    $"Inconsistent PFT mapping in {fileName}: " +
                    $"Individual {indivId} is mapped to '{pftName}' but was previously mapped to '{existingPft}' in file '{indivMappingsFile}'");
            }
            if (existingPft != pftName)
            {
                throw new InvalidDataException(
                    $"Inconsistent PFT mapping in {fileName}: " +
                    $"Individual {indivId} is mapped to '{pftName}' but was previously mapped to '{existingPft}' in file '{indivMappingsFile}'");
            }
        }
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
        
        _logger.LogWarning("Skipping stale output file (age: {age})",
            TimeUtils.FormatTimeSpan(age));
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
        string temporalResolution,
        bool dryRun)
    {
        var request = new CreateDatasetRequest
        {
            Name = name,
            Description = description,
            ModelVersion = repoInfo.CommitHash,
            ClimateDataset = climateDataset,
            SpatialResolution = spatialResolution,
            TemporalResolution = temporalResolution,
            CompressedParameters = CompressionUtility.CompressText(parameters),
            CompressedCodePatches = repoInfo.Patches
        };

        if (dryRun)
        {
            _logger.LogInformation("[DRY RUN] Dataset {name} will not be created", name);
            return -1;
        }

        var response = await _httpClient.PostAsJsonAsync(createEndpoint, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateDatasetResponse>();
        if (result == null)
            throw new InvalidOperationException("Server returned an empty response when creating dataset");
            
        return result.Id;
    }

    private async Task ImportOutputFile(int datasetId, string outputFile, bool dryRun)
    {
        // Parse output file (always do this, even if dry-run is enabled).
        Quantity quantity = await _parser.ParseOutputFileAsync(outputFile);

        // Validate PFT mappings if this is individual-level data
        if (quantity.Level == AggregationLevel.Individual && quantity.IndividualPfts != null)
            ValidateIndivPftMappings(quantity, Path.GetFileName(outputFile));

        if (dryRun)
        {
            _logger.LogInformation("[DRY RUN] File was parsed successfully, but will not be POST-ed");
            return;
        }

        _logger.LogInformation("Importing {File}", Path.GetFileName(outputFile));

        string endpoint = string.Format(addEndpoint, datasetId);
        var response = await _httpClient.PostAsJsonAsync(endpoint, quantity);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Import all output files from a gridded run and upload them to the DB.
    /// </summary>
    /// <param name="options">The options for the gridded import.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleGriddedImport(GriddedOptions options)
    {
        // Reset dataset-level state
        indivMappings = null;

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
            options.TemporalResolution,
            options.DryRun);

        // Process each output file, skipping stale ones
        foreach (string outputFile in outputFiles)
        {
            if (IsStaleFile(outputFile, mostRecentWriteTime))
            {
                EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                continue;
            }

            await ImportOutputFile(datasetId, outputFile, options.DryRun);
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
            using var __ = _logger.BeginScope(siteName);

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
            _logger.LogInformation("Creating dataset");
            int datasetId = await CreateDataset(
                options.Name,
                options.Description,
                parameters,
                repoInfo,
                options.ClimateDataset,
                siteLevelSpatialResolution,
                options.TemporalResolution,
                options.DryRun);

            // Process each output file, skipping stale ones
            foreach (string outputFile in outputFiles)
            {
                using var ___ = _logger.BeginScope(Path.GetFileName(outputFile));
                if (IsStaleFile(outputFile, mostRecentWriteTime))
                {
                    EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                    continue;
                }

                string variableName = Path.GetFileNameWithoutExtension(outputFile);
                await ImportOutputFile(datasetId, outputFile, options.DryRun);
            }
        }
    }
}
