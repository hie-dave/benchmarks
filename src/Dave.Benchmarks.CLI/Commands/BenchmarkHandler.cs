using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Commands;

public class BenchmarkHandler
{
    private readonly IApiClient api;
    private readonly ImportHandler importer;
    private readonly EvaluateHandler evaluator;
    private readonly ILogger<BenchmarkHandler> logger;

    public BenchmarkHandler(IApiClient api, ImportHandler importer, EvaluateHandler evaluator, ILogger<BenchmarkHandler> logger)
    {
        this.api = api; this.importer = importer; this.evaluator = evaluator; this.logger = logger;
    }

    public async Task RunAsync(BenchmarkOptions options, CancellationToken token = default)
    {
        int submissionId = await api.CreateBenchmarkSubmissionAsync(
            options.MergeRequestId, options.PipelineId, options.CommitSha, options.CommitMessage,
            options.SourceBranch, options.TargetBranch, options.BenchmarkName, token);
        logger.LogInformation("Using benchmark submission {SubmissionId}", submissionId);
        try
        {
            ImportResult imported = await importer.HandleSiteImport(new SiteOptions
            {
                RepoPath = options.RepoPath, Name = options.Name, Description = options.Description,
                ClimateDataset = options.ClimateDataset, TemporalResolution = options.TemporalResolution,
                BaselineChannel = options.BaselineChannel, CleanupOnFailure = options.CleanupOnFailure,
                SubmissionId = submissionId
            });
            logger.LogInformation("Imported {Count} datasets", imported.DatasetIds.Count);
            await api.CompleteBenchmarkSubmissionAsync(submissionId, token);
        }
        catch (Exception ex)
        {
            try { await api.FailBenchmarkSubmissionAsync(submissionId, ex.Message, token); }
            catch (Exception failure) { logger.LogWarning(failure, "Could not mark submission failed"); }
            throw;
        }
        await evaluator.RunAsync(new EvaluateOptions
        {
            SubmissionId = submissionId, Wait = true,
            TimeoutSeconds = options.TimeoutSeconds, PollIntervalSeconds = options.PollIntervalSeconds
        }, token);
    }
}
