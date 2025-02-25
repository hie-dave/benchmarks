using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// An interface to an API client.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Create a dataset group.
    /// </summary>
    /// <param name="name">Name of the group.</param>
    /// <param name="description">Description of the group.</param>
    /// <param name="metadata">Metadata for the group, encoded as JSON.</param>
    /// <returns>The ID of the created group.</returns>
    Task<int> CreateGroupAsync(
        string name,
        string description,
        string metadata = "{}");

    /// <summary>
    /// Create a level dataset.
    /// </summary>
    /// <param name="name">Name of the site.</param>
    /// <param name="description">Description of the site.</param>
    /// <param name="repoInfo">Information about the repository containing the model code.</param>
    /// <param name="climateDataset">Name of the climate dataset.</param>
    /// <param name="temporalResolution">Temporal resolution of the dataset.</param>
    /// <param name="metadata">Metadata for the dataset, encoded as JSON.</param>
    /// <param name="groupId">ID of the group to which this dataset belongs.</param>
    /// <returns>The ID of the created dataset.</returns>
    Task<int> CreateDatasetAsync(
        string name,
        string description,
        RepositoryInfo repoInfo,
        string climateDataset,
        string temporalResolution,
        string metadata,
        int? groupId = null);

    /// <summary>
    /// Add a quantity to a dataset.
    /// </summary>
    /// <param name="datasetId">ID of the dataset.</param>
    /// <param name="quantity">The quantity to be added.</param>
    /// <param name="dryRun">HTTP request will be performed iff this is false.</param>
    Task AddQuantityAsync(int datasetId, Quantity quantity);

    /// <summary>
    /// Flag a group as complete.
    /// </summary>
    /// <param name="groupId">ID of the group.</param>
    Task CompleteGroupAsync(int groupId);
}
