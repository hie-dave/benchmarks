using Dave.Benchmarks.Core.Services.Evaluation;

namespace Dave.Benchmarks.Web.Services.Evaluation;

public class EvaluationWorker : BackgroundService
{
    private readonly IEvaluationJobQueue queue;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<EvaluationWorker> logger;

    public EvaluationWorker(
        IEvaluationJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EvaluationWorker> logger)
    {
        this.queue = queue;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int runId;
            try
            {
                runId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IEvaluationEngine engine = scope.ServiceProvider.GetRequiredService<IEvaluationEngine>();
                await engine.ExecuteAsync(runId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception while executing evaluation run {RunId}", runId);
            }
        }
    }
}
