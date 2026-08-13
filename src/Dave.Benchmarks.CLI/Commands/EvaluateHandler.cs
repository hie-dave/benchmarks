using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Commands;

public class EvaluateHandler
{
    private readonly IApiClient apiClient;
    private readonly ILogger<EvaluateHandler> logger;

    public EvaluateHandler(IApiClient apiClient, ILogger<EvaluateHandler> logger)
    {
        this.apiClient = apiClient;
        this.logger = logger;
    }

    public async Task RunAsync(EvaluateOptions options, CancellationToken cancellationToken = default)
    {
        if (options.TimeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.TimeoutSeconds), "Timeout must be positive");
        if (options.PollIntervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.PollIntervalSeconds), "Poll interval must be positive");

        int runId = await apiClient.CreateEvaluationRunAsync(
            options.SubmissionId,
            cancellationToken);

        logger.LogInformation("Created evaluation run {EvaluationRunId}", runId);
        if (!options.Wait)
            return;

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        while (true)
        {
            EvaluationRun run;
            try
            {
                run = await apiClient.GetEvaluationRunAsync(runId, timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Evaluation run {runId} did not complete within {options.TimeoutSeconds} seconds");
            }

            switch (run.Status)
            {
                case EvaluationRunStatus.Succeeded when run.Passed == true:
                    logger.LogInformation("Evaluation run {EvaluationRunId} passed", runId);
                    return;
                case EvaluationRunStatus.Succeeded:
                    throw new EvaluationGateFailedException(runId);
                case EvaluationRunStatus.Failed:
                    throw new InvalidOperationException(
                        $"Evaluation run {runId} failed: {run.ErrorMessage ?? "unknown server error"}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Evaluation run {runId} did not complete within {options.TimeoutSeconds} seconds");
            }
        }
    }
}

public sealed class EvaluationGateFailedException : Exception
{
    public EvaluationGateFailedException(int evaluationRunId)
        : base($"Evaluation run {evaluationRunId} completed but did not pass the gate")
    {
        EvaluationRunId = evaluationRunId;
    }

    public int EvaluationRunId { get; }
}
