using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// Provides an API client which NO-OPs all requests.
/// </summary>
public class DryRunApiClient : IApiClient
{
    /// <summary>
    /// The logging service.
    /// </summary>
    private readonly ILogger<DryRunApiClient> logger;

    /// <summary>
    /// Creates a new instance of the DryRunApiClient.
    /// </summary>
    /// <param name="logger">The logging service.</param>
    public DryRunApiClient(ILogger<DryRunApiClient> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task AddQuantityAsync(int datasetId, Quantity quantity)
    {
        logger.LogInformation("[DRY RUN] Would add quantity {Quantity} to dataset {DatasetId}", quantity.Name, datasetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> CreateGroupAsync(string name, string description, string metadata)
    {
        logger.LogInformation("[DRY RUN] Would create group {Name} with description: {Description}", name, description);
        return Task.FromResult(1);
    }

    /// <inheritdoc />
    public Task<int> CreateDatasetAsync(string name, string description, RepositoryInfo repoInfo, string climateDataset, string temporalResolution, string metadata, int? groupId = null)
    {
        logger.LogInformation("[DRY RUN] Would create dataset {Name} ({ClimateDataset}, {TemporalResolution}) with description: {Description}", name, climateDataset, temporalResolution, description);
        return Task.FromResult(1);
    }

    /// <inheritdoc />
    public Task CompleteGroupAsync(int groupId)
    {
        logger.LogInformation("[DRY RUN] Would complete group {GroupId}", groupId);
        return Task.CompletedTask;
    }
}
