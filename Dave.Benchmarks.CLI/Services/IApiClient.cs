using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// An interface to an API client.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Create a site-level dataset.
    /// </summary>
    /// <param name="name">Name of the site.</param>
    /// <param name="description">Description of the site.</param>
    /// <param name="repoInfo">Information about the repository containing the model code.</param>
    /// <param name="climateDataset">Name of the climate dataset.</param>
    /// <param name="temporalResolution">Temporal resolution of the dataset.</param>
    /// <returns>The ID of the created dataset.</returns>
    Task<int> CreateDatasetAsync(
        string name,
        string description,
        RepositoryInfo repoInfo,
        string climateDataset,
        string temporalResolution);

    /// <summary>
    /// Upload a quantity from a site-level run to the server.
    /// </summary>
    /// <param name="datasetId">ID of the dataset to which this quantity belongs.</param>
    /// <param name="site">Name of the site.</param>
    /// <param name="insFile">Name of the instruction file.</param>
    /// <param name="latitude">Latitude of the site.</param>
    /// <param name="longitude">Longitude of the site.</param>
    /// <param name="dryRun">Whether to perform a dry run.</param>
    /// <returns>ID of the created site-level dataset.</returns>
    Task<int> CreateSiteDatasetAsync(
        int datasetId,
        string site,
        string insFile,
        double latitude,
        double longitude);

    /// <summary>
    /// Create a gridded dataset.
    /// </summary>
    /// <param name="name">Name of the dataset.</param>
    /// <param name="description">Description of the dataset.</param>
    /// <param name="datasetId">ID of the dataset to which this gridded run will be added.</param>
    /// <param name="repoInfo">Repository information for the dataset.</param>
    /// <param name="climateDataset">Climate dataset used in the dataset.</param>
    /// <param name="spatialResolution">Spatial resolution of the dataset.</param>
    /// <param name="temporalResolution">Temporal resolution of the dataset.</param>
    /// <returns>The ID of the created dataset.</returns>
    Task<int> CreateGriddedDatasetAsync(
        string name,
        string description,
        int datasetId,
        RepositoryInfo repoInfo,
        string climateDataset,
        string spatialResolution,
        string temporalResolution);

    /// <summary>
    /// Add a quantity to a dataset.
    /// </summary>
    /// <param name="datasetId">ID of the dataset.</param>
    /// <param name="quantity">The quantity to be added.</param>
    /// <param name="dryRun">HTTP request will be performed iff this is false.</param>
    Task AddQuantityAsync(int datasetId, Quantity quantity);
}
