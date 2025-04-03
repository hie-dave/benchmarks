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
    private enum Controller
    {
        Data,
        Predictions
    }

    /// <summary>
    /// Endpoint of the predictions API.
    /// </summary>
    private const string predictionsApi = "api/predictions";

    /// <summary>
    /// Endpoint of the data API.
    /// </summary>
    private const string dataApi = "api/data";

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
    /// API endpoint used to delete a dataset group.
    /// </summary>
    private const string deleteGroupEndpoint = "group/{0}";

    /// <summary>
    /// API endpoint used to create a variable.
    /// </summary>
    private const string createVariableEndpoint = "{0}/variables";

    /// <summary>
    /// API endpoint used to create a layer.
    /// </summary>
    private const string createLayerEndpoint = "variables/{0}/layers";

    /// <summary>
    /// API endpoint used to append data to a layer.
    /// </summary>
    private const string appendDataEndpoint = "layers/{0}/data";

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
        // Create the variable first
        CreateVariableRequest createRequest = new CreateVariableRequest()
        {
            Name = quantity.Name,
            Description = quantity.Description,
            Level = quantity.Level,
            Units = quantity.Layers.First().Unit.Name,
            IndividualPfts = quantity.IndividualPfts
        };

        int variableId = await CreateVariableAsync(datasetId, createRequest);

        // Process each layer
        foreach (Layer layer in quantity.Layers)
        {
            // Create the layer
            CreateLayerRequest layerRequest = new CreateLayerRequest
            {
                Name = layer.Name,
                Description = layer.Name // TODO: Add layer-level descriptions?
            };

            int layerId = await CreateLayerAsync(variableId, layerRequest);

            // Split data points into manageable chunks (1000 points per request)
            const int chunkSize = 1000;
            for (int i = 0; i < layer.Data.Count; i += chunkSize)
            {
                List<DataPoint> chunk = layer.Data.Skip(i).Take(chunkSize).ToList();
                await AppendDataAsync(layerId, new AppendDataRequest { DataPoints = chunk });
                
                logger.LogInformation(
                    "Uploaded {Current} of {Total} data points for layer {Layer} in variable {Variable}",
                    Math.Min(i + chunkSize, layer.Data.Count),
                    layer.Data.Count,
                    layer.Name,
                    quantity.Name);
            }
        }
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
    public async Task DeleteGroupAsync(int groupId)
    {
        string endpoint = string.Format(deleteGroupEndpoint, groupId);
        await DeleteAsync(endpoint, Controller.Data);
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
    /// Create a variable.
    /// </summary>
    /// <param name="datasetId">ID of the dataset.</param>
    /// <param name="request">The variable creation request.</param>
    /// <returns>The ID of the created variable.</returns>
    public async Task<int> CreateVariableAsync(int datasetId, CreateVariableRequest request)
    {
        string endpoint = string.Format(createVariableEndpoint, datasetId);
        var response = await PostAsync(endpoint, request);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    /// <summary>
    /// Create a layer.
    /// </summary>
    /// <param name="variableId">ID of the variable.</param>
    /// <param name="request">The layer creation request.</param>
    /// <returns>The ID of the created layer.</returns>
    public async Task<int> CreateLayerAsync(int variableId, CreateLayerRequest request)
    {
        string endpoint = string.Format(createLayerEndpoint, variableId);
        var response = await PostAsync(endpoint, request);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    /// <summary>
    /// Append data to a layer.
    /// </summary>
    /// <param name="layerId">ID of the layer.</param>
    /// <param name="request">The data to append.</param>
    public async Task AppendDataAsync(int layerId, AppendDataRequest request)
    {
        string endpoint = string.Format(appendDataEndpoint, layerId);
        await PostAsync(endpoint, request);
    }

    /// <summary>
    /// Performs a POST request to the specified endpoint with no body.
    /// </summary>
    /// <param name="endpoint">The API endpoint, relative to the predictions API base URL.</param>
    private async Task<HttpResponseMessage> PostAsync(string endpoint, Controller controller = Controller.Predictions)
    {
        endpoint = $"{GetControllerRoute(controller)}/{endpoint}";
        logger.LogDebug("Sending POST request to {endpoint}", endpoint);

        var response = await httpClient.PostAsync(endpoint, null);
        await ValidateResponseAsync(response);
        return response;
    }

    /// <summary>
    /// Performs a POST request to the specified endpoint with no body.
    /// </summary>
    /// <param name="endpoint">The API endpoint, relative to the predictions API base URL.</param>
    private async Task<HttpResponseMessage> DeleteAsync(string endpoint, Controller controller = Controller.Predictions, CancellationToken cancellationToken = default)
    {
        endpoint = $"{GetControllerRoute(controller)}/{endpoint}";
        logger.LogDebug("Sending DELETE request to {endpoint}", endpoint);

        var response = await httpClient.DeleteAsync(endpoint, cancellationToken);
        await ValidateResponseAsync(response);
        return response;
    }

    /// <summary>
    /// Performs a POST request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint, relative to the predictions API base URL.</param>
    /// <param name="request">The object to be POSTed.</param>
    private async Task<HttpResponseMessage> PostAsync(string endpoint, object request, Controller controller = Controller.Predictions)
    {
        endpoint = $"{GetControllerRoute(controller)}/{endpoint}";

        logger.LogDebug("Sending POST request to {endpoint}", endpoint);

        // TODO: use human-readable sizes, not necessarily MiB.
        double size = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(request)) / (1024.0 * 1024.0);
        logger.LogDebug("Request size: {size:f1}MiB", size);

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

    /// <summary>
    /// Get the route to the specified controller.
    /// </summary>
    /// <param name="controller">The controller type.</param>
    /// <returns>The route to the controller.</returns>
    /// <exception cref="ArgumentException">Thrown for unknown controller types.</exception>
    private string GetControllerRoute(Controller controller)
    {
        return controller switch
        {
            Controller.Data => dataApi,
            Controller.Predictions => predictionsApi,
            _ => throw new ArgumentException(nameof(controller))
        };
    }
}
