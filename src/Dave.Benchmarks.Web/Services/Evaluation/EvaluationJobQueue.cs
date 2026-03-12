using System.Threading.Channels;

namespace Dave.Benchmarks.Web.Services.Evaluation;

public class EvaluationJobQueue : IEvaluationJobQueue
{
    private readonly Channel<int> queue = Channel.CreateUnbounded<int>();

    public ValueTask EnqueueAsync(int evaluationRunId, CancellationToken cancellationToken = default)
    {
        return queue.Writer.WriteAsync(evaluationRunId, cancellationToken);
    }

    public ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
    {
        return queue.Reader.ReadAsync(cancellationToken);
    }
}
