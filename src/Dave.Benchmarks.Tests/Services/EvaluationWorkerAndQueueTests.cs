using System.Collections.Concurrent;
using Dave.Benchmarks.Core.Services.Evaluation;
using Dave.Benchmarks.Web.Services.Evaluation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class EvaluationWorkerAndQueueTests
{
    [Fact]
    public async Task EvaluationJobQueue_IsFifo()
    {
        IEvaluationJobQueue queue = new EvaluationJobQueue();
        await queue.EnqueueAsync(10);
        await queue.EnqueueAsync(20);

        int first = await queue.DequeueAsync(CancellationToken.None);
        int second = await queue.DequeueAsync(CancellationToken.None);

        Assert.Equal(10, first);
        Assert.Equal(20, second);
    }

    [Fact]
    public async Task EvaluationWorker_ProcessesQueuedRuns()
    {
        IEvaluationJobQueue queue = new EvaluationJobQueue();
        RecordingEngine engine = new();

        ServiceCollection services = new();
        services.AddScoped<IEvaluationEngine>(_ => engine);
        IServiceProvider provider = services.BuildServiceProvider();

        EvaluationWorker worker = new(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<ILogger<EvaluationWorker>>());

        using CancellationTokenSource cts = new();
        await worker.StartAsync(cts.Token);

        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);

        await WaitUntilAsync(() => engine.Calls.Count >= 2, TimeSpan.FromSeconds(3));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal([1, 2], engine.Calls.ToArray());
    }

    [Fact]
    public async Task EvaluationWorker_ContinuesAfterEngineException()
    {
        IEvaluationJobQueue queue = new EvaluationJobQueue();
        RecordingEngine engine = new(throwOnRunId: 1);

        ServiceCollection services = new();
        services.AddScoped<IEvaluationEngine>(_ => engine);
        IServiceProvider provider = services.BuildServiceProvider();

        EvaluationWorker worker = new(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<ILogger<EvaluationWorker>>());

        using CancellationTokenSource cts = new();
        await worker.StartAsync(cts.Token);

        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);

        await WaitUntilAsync(() => engine.Calls.Count >= 2, TimeSpan.FromSeconds(3));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal([1, 2], engine.Calls.ToArray());
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(20);
        }

        throw new TimeoutException("Condition was not met within timeout.");
    }

    private sealed class RecordingEngine : IEvaluationEngine
    {
        private readonly int? throwOnRunId;
        public ConcurrentQueue<int> Calls { get; } = new();

        public RecordingEngine(int? throwOnRunId = null)
        {
            this.throwOnRunId = throwOnRunId;
        }

        public Task ExecuteAsync(int evaluationRunId, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(evaluationRunId);
            if (throwOnRunId == evaluationRunId)
                throw new InvalidOperationException("boom");
            return Task.CompletedTask;
        }
    }
}
