using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Services;
using LpjGuess.Core.Models.Importer;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// Provides an API client which NO-OPs all requests.
/// </summary>
public class DryRunApiClient : IApiClient
{
    public Task<int> CreateObservationGroupAsync(string name, string source, string version, string description, DatasetGroupKind kind, string metadata, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<int> CreateObservationDatasetAsync(int groupId, string name, string description, string temporalResolution, string simulationId, MatchingStrategy strategy, int? maxDistance, string metadata, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<int> CreateObservationVariableAsync(int datasetId, CreateVariableRequest request, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<int> CreateObservationLayerAsync(int variableId, CreateLayerRequest request, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task AppendObservationDataAsync(int layerId, AppendObservationDataRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CompleteObservationGroupAsync(int groupId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ActivateObservationGroupAsync(int groupId, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
    public Task<int> CreateDatasetAsync(string name, string description, RepositoryInfo repoInfo, string climateDataset, string temporalResolution, string simulationId, string baselineChannel, string metadata, int? groupId = null) =>
        CreateDatasetAsync(name, description, repoInfo, climateDataset, temporalResolution, simulationId, baselineChannel, metadata, groupId, null);

    public Task<int> CreateDatasetAsync(string name, string description, RepositoryInfo repoInfo, string climateDataset, string temporalResolution, string simulationId, string baselineChannel, string metadata, int? groupId, int? benchmarkSubmissionId)
    {
        logger.LogInformation(
            "[DRY RUN] Would create dataset {Name} ({ClimateDataset}, {TemporalResolution}, simulation={SimulationId}, channel={BaselineChannel}) with description: {Description}",
            name,
            climateDataset,
            temporalResolution,
            simulationId,
            baselineChannel,
            description);
        return Task.FromResult(1);
    }

    /// <inheritdoc />
    public Task CompleteGroupAsync(int groupId)
    {
        logger.LogInformation("[DRY RUN] Would complete group {GroupId}", groupId);
        return Task.CompletedTask;
    }

    public Task DeleteGroupAsync(int groupId)
    {
        logger.LogInformation("[DRY RUN] Would delete group {GroupId}", groupId);
        return Task.CompletedTask;
    }

    public Task<int> CreateVariableAsync(int datasetId, CreateVariableRequest request)
    {
        logger.LogInformation(
            "[DRY RUN] Would create variable {Name} in dataset {DatasetId} with level {Level} and units {Units}",
            request.Name,
            datasetId,
            request.Level,
            request.Units);

        return Task.FromResult(1); // Return dummy ID
    }

    public Task<int> CreateLayerAsync(int variableId, CreateLayerRequest request)
    {
        logger.LogInformation(
            "[DRY RUN] Would create layer {Name} in variable {VariableId}",
            request.Name,
            variableId);

        return Task.FromResult(1); // Return dummy ID
    }

    public Task AppendDataAsync(int layerId, AppendDataRequest request)
    {
        logger.LogInformation(
            "[DRY RUN] Would append {Count} data points to layer {LayerId}",
            request.DataPoints.Count,
            layerId);

        return Task.CompletedTask;
    }

    public Task<int> CreateEvaluationRunAsync(
        int benchmarkSubmissionId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[DRY RUN] Would evaluate dataset {DatasetId} for merge request {MergeRequestId}",
            benchmarkSubmissionId,
            "dry-run");
        return Task.FromResult(-1);
    }

    public Task<int> CreateBenchmarkSubmissionAsync(string mergeRequestId, string pipelineId, string commitSha, string? commitMessage, string sourceBranch, string targetBranch, string benchmarkName, CancellationToken cancellationToken = default) => Task.FromResult(-1);
    public Task CompleteBenchmarkSubmissionAsync(int submissionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task FailBenchmarkSubmissionAsync(int submissionId, string error, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<EvaluationRun> GetEvaluationRunAsync(
        int evaluationRunId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Dry-run evaluation polling is not supported");
}
