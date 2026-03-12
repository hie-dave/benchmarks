namespace Dave.Benchmarks.Web.Services.Evaluation;

public interface IEvaluationJobQueue
{
    ValueTask EnqueueAsync(int evaluationRunId, CancellationToken cancellationToken = default);

    ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
}
