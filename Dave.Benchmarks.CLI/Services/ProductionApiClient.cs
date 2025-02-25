using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;
using Dave.Benchmarks.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// API client for interacting with the production API.
/// </summary>
public class ProductionApiClient : IApiClient
{
    /// <summary>
    /// Endpoint of the predictions API.
    /// </summary>
    private const string predictionsApi = "api/predictions";

    /// <summary>
    /// API endpoint used to create a dataset group.
    /// </summary>
    private const string createGroupEndpoint = "groups/create";

    /// <summary>
    /// API endpoint used to upload data to a quantity.
    /// </summary>
    private const string addQuantityEndpoint = "{0}/add";

    /// <summary>
    /// API endpoint used to flag a group as complete.
    /// </summary>
    private const string completeGroupEndpoint = "groups/{0}/complete";

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

    /// <summary>
    /// Flag a group as complete.
    /// </summary>
    /// <param name="groupId">ID of the group.</param>
    public async Task CompleteGroupAsync(int groupId)
    {
        string endpoint = string.Format(completeGroupEndpoint, groupId);
        await PostAsync(endpoint);
    }

    /// <summary>
    /// Create a dataset group.
    /// </summary>
    /// <param name="name">Name of the group.</param>
    /// <param name="description">Description of the group.</param>
    /// <param name="metadata">Metadata for the group, encoded as JSON.</param>
    /// <returns>The ID of the created group.</returns>
    public async Task<int> CreateGroupAsync(
        string name,
        string description,
        string metadata)
    {
        CreateDatasetGroupRequest request = new CreateDatasetGroupRequest()
        {
            Name = name,
            Description = description,
            Metadata = metadata
        };

        string endpoint = createGroupEndpoint;
        var response = await PostAsync(endpoint, request);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    /// <inheritdoc />
    public async Task<int> CreateDatasetAsync(
        string name,
        string description,
        RepositoryInfo repoInfo,
        string climateDataset,
        string temporalResolution,
        string metadata,
        int? groupId = null)
    {
        CreateDatasetRequest request = new CreateDatasetRequest()
        {
            Name = name,
            Description = description,
            ModelVersion = repoInfo.CommitHash,
            ClimateDataset = climateDataset,
            TemporalResolution = temporalResolution,
            CompressedCodePatches = repoInfo.Patches,
            Metadata = metadata,
            GroupId = groupId
        };

        var response = await PostAsync(createEndpoint, request);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    /// <summary>
    /// Performs a POST request to the specified endpoint with no body.
    /// </summary>
    /// <param name="endpoint">The API endpoint, relative to the predictions API base URL.</param>
    private async Task<HttpResponseMessage> PostAsync(string endpoint)
    {
        endpoint = $"{predictionsApi}/{endpoint}";
        logger.LogDebug("Sending POST request to {endpoint}", endpoint);

        var response = await httpClient.PostAsync(endpoint, null);
        await ValidateResponseAsync(response);
        return response;
    }

    /// <summary>
    /// Performs a POST request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint, relative to the predictions API base URL.</param>
    /// <param name="request">The object to be POSTed.</param>
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
