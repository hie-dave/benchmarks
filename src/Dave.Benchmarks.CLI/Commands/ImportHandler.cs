using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Services;
using ExtendedXmlSerializer;
using LpjGuess.Core.Helpers;
using LpjGuess.Core.Models;
using LpjGuess.Core.Models.Entities;
using LpjGuess.Core.Models.Importer;
using LpjGuess.Core.Parsers;
using LpjGuess.Core.Services;
using LpjGuess.Core.Utility;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Commands;

/// <summary>
/// Handles importing model predictions from output files to the database.
/// </summary>
public class ImportHandler
{
    private readonly ILogger<ImportHandler> logger;
    private readonly IModelOutputParser parser;
    private readonly IGitService git;
    private readonly IApiClient apiClient;
    private readonly IOutputFileTypeResolver resolver;
    private readonly IGridlistParser gridlistParser;
    private readonly IFileSystem fileSystem;

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

    /// <summary>
    /// Logger for InstructionFileHelper.
    /// </summary>
    private readonly ILogger<InstructionFileHelper> instructionHelperLogger;

    /// <summary>
    /// Factory for creating instruction file parsers.
    /// </summary>
    private readonly IInstructionFileParserFactory instructionFileParserFactory;

    public ImportHandler(
        ILogger<ImportHandler> logger,
        IModelOutputParser parser,
        IGitService gitService,
        IApiClient apiClient,
        IOutputFileTypeResolver resolver,
        IGridlistParser gridlistParser,
        ILogger<InstructionFileHelper> instructionHelperLogger,
        IFileSystem fileSystem,
        IInstructionFileParserFactory instructionFileParserFactory)
    {
        this.logger = logger;
        this.parser = parser;
        git = gitService;
        this.apiClient = apiClient;
        this.resolver = resolver;
        this.gridlistParser = gridlistParser;
        this.instructionHelperLogger = instructionHelperLogger;
        this.fileSystem = fileSystem;
        this.instructionFileParserFactory = instructionFileParserFactory;
    }

    /// <summary>
    /// Gets the most recent write time from a set of output files
    /// </summary>
    /// <param name="outputFiles">The files to check.</param>
    /// <returns>The most recent write time.</returns>
    private DateTime GetMostRecentWriteTime(string[] outputFiles)
    {
        return outputFiles.Max(fileSystem.GetLastWriteTime);
    }

    /// <summary>
    /// Checks if a file is stale by comparing its write time to the most recent write time
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="mostRecentWriteTime">The most recent write time.</param>
    /// <returns>True if the file is stale, false otherwise.</returns>
    private bool IsStaleFile(string filePath, DateTime mostRecentWriteTime)
    {
        DateTime lastWriteTime = fileSystem.GetLastWriteTime(filePath);
        TimeSpan age = mostRecentWriteTime - lastWriteTime;

        return age.TotalSeconds > staleFileThresholdSeconds;
    }

    /// <summary>
    /// Emits a warning for a stale file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="mostRecentWriteTime">The most recent write time.</param>
    private void EmitStaleFileWarning(string filePath, DateTime mostRecentWriteTime)
    {
        DateTime lastWriteTime = fileSystem.GetLastWriteTime(filePath);
        TimeSpan age = mostRecentWriteTime - lastWriteTime;

        logger.LogWarning("Skipping stale output file (age: {age})",
            TimeUtils.FormatTimeSpan(age));
    }

    private string[] EnumerateOutputFiles(string directory)
    {
        const SearchOption opts = SearchOption.TopDirectoryOnly;
        return fileSystem.EnumerateFiles(directory, "*.out", opts).ToArray();
    }

    /// <summary>
    /// Import all output files from a gridded run and upload them to the DB.
    /// </summary>
    /// <param name="options">The options for the gridded import.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleGriddedImport(GriddedOptions options)
    {
        // Get repository info
        using var _ = logger.BeginScope("importer");
        RepositoryInfo repoInfo = git.GetRepositoryInfo(options.RepoPath);

        // Parse instruction file
        IInstructionFileParser parser = instructionFileParserFactory.Create(options.InstructionFile);

        // Get all output files and find most recent write time
        string[] outputFiles = EnumerateOutputFiles(options.OutputDir);
        if (outputFiles.Length == 0)
            return;

        // Build the lookup table for this site's output files
        resolver.BuildLookupTable(parser);

        DateTime mostRecentWriteTime = GetMostRecentWriteTime(outputFiles);

        // Create group.
        // TODO: group ID should be an optional user input.
        int groupId = await apiClient.CreateGroupAsync(
            options.Name,
            options.Description); // TODO: metadata

        try
        {
            // Create dataset
            int datasetId = await apiClient.CreateDatasetAsync(
                options.Name,
                options.Description,
                repoInfo,
                options.ClimateDataset,
                options.TemporalResolution,
                options.SimulationId,
                options.BaselineChannel,
                "{}", // TODO: metadata
                groupId);

            // Process each output file, skipping stale ones
            foreach (string outputFile in outputFiles)
            {
                if (IsStaleFile(outputFile, mostRecentWriteTime))
                {
                    EmitStaleFileWarning(outputFile, mostRecentWriteTime);
                    continue;
                }

                // Parse the output file.
                Quantity quantity = await this.parser.ParseOutputFileAsync(outputFile);

                // Add it to the dataset.
                await apiClient.AddQuantityAsync(datasetId, quantity);
            }
        }
        catch
        {
            // Delete group (this will also delete all datasets in the group).
            logger.LogInformation("Import failed; deleting group {GroupId}", groupId);
            await apiClient.DeleteGroupAsync(groupId);
            throw;
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

        // Create group.
        int groupId = await apiClient.CreateGroupAsync(
            options.Name,
            options.Description); // TODO: metadata

        try
        {
            foreach ((string siteName, string instructionFile, string outputDir) in runs)
            {
                using var __ = logger.BeginScope(siteName);

                // Parse instruction file
                IInstructionFileParser insParser = instructionFileParserFactory.Create(instructionFile);
                InstructionFileHelper helper = new InstructionFileHelper(insParser, instructionHelperLogger);
                string gridlist = helper.GetGridlist();
                IEnumerable<Gridcell> gridcells = await gridlistParser.ParseAsync(gridlist);
                if (gridcells.Count() > 1)
                    ExceptionHelper.Throw<InvalidDataException>(logger, $"Parser error: site {siteName} has more than one coordinate");

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

                Gridcell gridcell = gridcells.Single();
                logger.LogInformation("Creating site-level dataset");
                int datasetId = await apiClient.CreateDatasetAsync(
                    siteName,
                    $"{options.Name} - {siteName}",
                    repoInfo,
                    options.ClimateDataset,
                    options.TemporalResolution,
                    // Use site name as simulation ID for site-level runs.
                    siteName,
                    options.BaselineChannel,
                    "{}", // TODO: metadata
                    groupId);

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
                    await apiClient.AddQuantityAsync(datasetId, quantity);
                }
            }
        }
        catch
        {
            // Delete group (this will also delete all datasets in the group).
            logger.LogInformation("Import failed; deleting group {GroupId}", groupId);
            await apiClient.DeleteGroupAsync(groupId);
            throw;
        }
    }
}
