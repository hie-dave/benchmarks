
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;
using Dave.Benchmarks.Core.Utilities;
using Microsoft.Extensions.Logging;

public class ProductionApiClient : IApiClient
{
    /// <summary>
    /// Endpoint of the predictions API.
    /// </summary>
    private const string predictionsApi = "api/predictions";

    /// <summary>
    /// API endpoint used to upload data to a quantity.
    /// </summary>
    private const string addQuantityEndpoint = "{0}/add";

    /// <summary>
    /// API endpoint used to add a site run to a dataset.
    /// </summary>
    private const string addSiteEndpoint = "site/{0}/add";

    /// <summary>
    /// API endpoint used to add a gridded run to a dataset.
    /// </summary>
    private const string addGriddedEndpoint = "gridded/{0}/add";

    /// <summary>
    /// API endpoint used to create a dataset.
    /// </summary>
    private const string createEndpoint = "create";

    /// <summary>
    /// The HTTP client used to communicate with the predictions API.
    /// </summary>
    private readonly HttpClient httpClient;

    /// <summary>
    /// The logging service.
    /// </summary>
    private readonly ILogger<ProductionApiClient> logger;

    /// <summary>
    /// Creates a new instance of the ProductionApiClient.
    /// </summary>
    /// <param name="logger">The logging service.</param>
    /// <param name="httpClient">The HTTP client used to communicate with the predictions API.</param>
    public ProductionApiClient(ILogger<ProductionApiClient> logger, HttpClient httpClient)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task AddQuantityAsync(int datasetId, Quantity quantity)
    {
        string endpoint = string.Format(addQuantityEndpoint, datasetId);
        await PostAsync(endpoint, quantity);
    }

    /// <inheritdoc />
    public async Task<int> CreateDatasetAsync(
        string name,
        string description,
        RepositoryInfo repoInfo,
        string climateDataset,
        string temporalResolution)
    {
        CreateSiteDatasetRequest request = new CreateSiteDatasetRequest()
        {
            Name = name,
            Description = description,
            ModelVersion = repoInfo.CommitHash,
            ClimateDataset = climateDataset,
            TemporalResolution = temporalResolution,
            CompressedCodePatches = repoInfo.Patches
        };

        var response = await PostAsync(createEndpoint, request);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    /// <inheritdoc />
    public async Task<int> CreateSiteDatasetAsync(
        int datasetId,
        string site,
        string insFile,
        double latitude,
        double longitude)
    {
        AddSiteRequest request = new AddSiteRequest()
        {
            Name = site,
            InstructionFile = CompressionUtility.CompressText(insFile),
            Latitude = latitude,
            Longitude = longitude
        };

        string endpoint = string.Format(addSiteEndpoint, datasetId);
        var response = await PostAsync(endpoint, request);

        return await response.Content.ReadFromJsonAsync<int>();
    }

    /// <inheritdoc />
    public async Task<int> CreateGriddedDatasetAsync(
        string name,
        string description,
        int datasetId,
        RepositoryInfo repoInfo,
        string climateDataset,
        string spatialResolution,
        string temporalResolution)
    {
        CreateGriddedDatasetRequest request = new CreateGriddedDatasetRequest()
        {
            Name = name,
            Description = description,
            ModelVersion = repoInfo.CommitHash,
            ClimateDataset = climateDataset,
            TemporalResolution = temporalResolution,
            CompressedCodePatches = repoInfo.Patches,
            SpatialResolution = spatialResolution
        };

        string endpoint = string.Format(addGriddedEndpoint, datasetId);
        var response = await PostAsync(endpoint, request);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    /// <summary>
    /// Performs a POST request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint, relative to the predictions API base URL.</param>
    /// <param name="request">The obejct to be POSTed.</param>
    private async Task<HttpResponseMessage> PostAsync(string endpoint, object request)
    {
        endpoint = $"{predictionsApi}/{endpoint}";

        logger.LogDebug("Sending POST request to {endpoint}", endpoint);
        logger.LogDebug("Request size: {size}MiB", () => Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(request)) / (1024.0 * 1024.0));

        var response = await httpClient.PostAsJsonAsync(endpoint, request);
        await ValidateResponseAsync(response);
        return response;
    }

    /// <summary>
    /// Check a response from the server and throw an exception if it's not OK.
    /// </summary>
    /// <param name="response">The response to check.</param>
    private async Task ValidateResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            string content = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Server returned {Code}: {Content}", response.StatusCode, content);
        }
        response.EnsureSuccessStatusCode();
    }
}
