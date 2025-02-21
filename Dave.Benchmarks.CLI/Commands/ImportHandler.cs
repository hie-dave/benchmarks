using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dave.Benchmarks.CLI.Models;
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
    private readonly ILogger<ImportHandler> logger;
    private readonly ModelOutputParser parser;
    private readonly GitService git;
    private readonly InstructionFileParser insParser;
    private readonly IApiClient apiClient;
    private readonly IOutputFileTypeResolver resolver;
    private readonly GridlistParser gridlistParser;

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
        IApiClient apiClient,
        IOutputFileTypeResolver resolver,
        GridlistParser gridlistParser)
    {
        this.logger = logger;
        this.parser = parser;
        git = gitService;
        insParser = instructionParser;
        this.apiClient = apiClient;
        this.resolver = resolver;
        this.gridlistParser = gridlistParser;
    }

    private void ValidateIndivPftMappings(Quantity quantity, string fileName)
    {
        if (quantity.Level != AggregationLevel.Individual)
        {
            if (quantity.IndividualPfts != null)
                ExceptionHelper.Throw<InvalidDataException>(logger, $"File {fileName} is not an indiv-level output, and should therefore not contain indiv-pft mappings");
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
            ExceptionHelper.Throw<InvalidDataException>(logger, $"File {fileName} is an indiv-level output, but does not contain indiv-pft mappings");

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
        
        logger.LogWarning("Skipping stale output file (age: {age})",
            TimeUtils.FormatTimeSpan(age));
    }

    private string[] EnumerateOutputFiles(string directory)
    {
        return Directory.GetFiles(directory, "*.out");
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
        using var _ = logger.BeginScope("importer");
        RepositoryInfo repoInfo = git.GetRepositoryInfo(options.RepoPath);

        // Parse instruction file
        string parameters = await insParser.ParseInstructionFileAsync(options.InstructionFile);

        // Get all output files and find most recent write time
        string[] outputFiles = Directory.GetFiles(options.OutputDir, "*.out");
        if (outputFiles.Length == 0)
            return;

        DateTime mostRecentWriteTime = GetMostRecentWriteTime(outputFiles);

        int datasetId = await apiClient.CreateDatasetAsync(
            options.Name,
            options.Description,
            repoInfo,
            options.ClimateDataset,
            options.TemporalResolution
        );

        // Create dataset
        int gridId = await apiClient.CreateGriddedDatasetAsync(
            options.Name,
            options.Description,
            datasetId,
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

            // Parse the output file.
            Quantity quantity = await parser.ParseOutputFileAsync(outputFile);

            // Add it to the dataset.
            await apiClient.AddQuantityAsync(gridId, quantity);
        }
    }

    public async Task HandleSiteImport(SiteOptions options)
    {
        using var _ = logger.BeginScope("importer");

        // Find all site-level runs
        IEnumerable<(string, string, string)> runs = git.FindSiteLevelRuns(options.RepoPath).ToList();
        if (!runs.Any())
            throw new InvalidOperationException("No site-level runs found in repository");

        // Get repository info
        RepositoryInfo repoInfo = git.GetRepositoryInfo(options.RepoPath);

        // Get time stamp of most recently-written output file.
        IEnumerable<string> allFiles = runs.SelectMany(r => EnumerateOutputFiles(r.Item3));
        DateTime globalTimestamp = GetMostRecentWriteTime(allFiles.ToArray());

        // Create dataset
        int datasetId = await apiClient.CreateDatasetAsync(
            options.Name,
            options.Description,
            repoInfo,
            options.ClimateDataset,
            options.TemporalResolution);

        foreach ((string siteName, string instructionFile, string outputDir) in runs)
        {
            using var __ = logger.BeginScope(siteName);

            // Parse instruction file
            string parameters = await insParser.ParseInstructionFileAsync(instructionFile);
            string gridlist = insParser.GetGridlist();
            IEnumerable<Coordinate> coordinates = await gridlistParser.Parse(gridlist);
            if (coordinates.Count() > 1)
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Parser error: site {siteName} has more than one coordinate");

            Coordinate coordinate = coordinates.Single();
            logger.LogInformation("Creating site-level dataset");
            int siteId = await apiClient.CreateSiteDatasetAsync(
                datasetId,
                siteName,
                parameters,
                coordinate.Latitude,
                coordinate.Longitude);

            // Build the lookup table for this site's output files
            resolver.BuildLookupTable(insParser);

            // Get all output files and find most recent write time
            string[] outputFiles = EnumerateOutputFiles(outputDir);
            if (!outputFiles.Any())
            {
                // GetMostRecentWriteTime() will throw on empty collections.
                logger.LogWarning("Site {siteName} has no output files", siteName);
                continue;
            }

            // Get most recent write time for this site.
            DateTime mostRecentWriteTime = GetMostRecentWriteTime(outputFiles);

            if ((globalTimestamp - mostRecentWriteTime).TotalSeconds > staleSiteThresholdSeconds)
            {
                logger.LogWarning("Site {siteName} is stale (age: {age})", siteName, TimeUtils.FormatTimeSpan(globalTimestamp - mostRecentWriteTime));
                continue;
            }

            // Process each output file, skipping stale ones
            foreach (string outputFile in outputFiles)
            {
                using var ___ = logger.BeginScope(Path.GetFileName(outputFile));
                if (IsStaleFile(outputFile, mostRecentWriteTime))
                {
                    EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                    continue;
                }

                string variableName = Path.GetFileNameWithoutExtension(outputFile);

                // Parse the output file.
                Quantity quantity = await parser.ParseOutputFileAsync(outputFile);

                // Add it to the dataset.
                await apiClient.AddQuantityAsync(siteId, quantity);
            }
        }
    }
}
