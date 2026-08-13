using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Models.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Dave.Benchmarks.Tests.CLI;

public class EvaluateHandlerTests
{
    [Fact]
    public async Task RunAsync_WithoutWait_SubmitsOnly()
    {
        Mock<IApiClient> api = CreateApi();
        EvaluateHandler handler = new(api.Object, NullLogger<EvaluateHandler>.Instance);

        await handler.RunAsync(Options(wait: false));

        api.Verify(a => a.CreateEvaluationRunAsync(42, It.IsAny<CancellationToken>()));
        api.Verify(a => a.GetEvaluationRunAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WithWait_ReturnsWhenRunPasses()
    {
        Mock<IApiClient> api = CreateApi();
        api.SetupSequence(a => a.GetEvaluationRunAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationRun { Status = EvaluationRunStatus.Running })
            .ReturnsAsync(new EvaluationRun { Status = EvaluationRunStatus.Succeeded, Passed = true });
        EvaluateHandler handler = new(api.Object, NullLogger<EvaluateHandler>.Instance);

        EvaluateOptions options = Options(wait: true);
        options.PollIntervalSeconds = 1;
        await handler.RunAsync(options);

        api.Verify(a => a.GetEvaluationRunAsync(10, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_WithWait_ThrowsWhenGateFails()
    {
        Mock<IApiClient> api = CreateApi();
        api.Setup(a => a.GetEvaluationRunAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationRun { Status = EvaluationRunStatus.Succeeded, Passed = false });
        EvaluateHandler handler = new(api.Object, NullLogger<EvaluateHandler>.Instance);

        EvaluationGateFailedException exception = await Assert.ThrowsAsync<EvaluationGateFailedException>(
            () => handler.RunAsync(Options(wait: true)));

        Assert.Equal(10, exception.EvaluationRunId);
    }

    [Fact]
    public async Task RunAsync_WithWait_ThrowsWhenServerRunFails()
    {
        Mock<IApiClient> api = CreateApi();
        api.Setup(a => a.GetEvaluationRunAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationRun
            {
                Status = EvaluationRunStatus.Failed,
                Passed = false,
                ErrorMessage = "database unavailable"
            });
        EvaluateHandler handler = new(api.Object, NullLogger<EvaluateHandler>.Instance);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.RunAsync(Options(wait: true)));

        Assert.Contains("database unavailable", exception.Message);
    }

    private static Mock<IApiClient> CreateApi()
    {
        Mock<IApiClient> api = new();
        api.Setup(a => a.CreateEvaluationRunAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        return api;
    }

    private static EvaluateOptions Options(bool wait) => new()
    {
        SubmissionId = 42,
        Wait = wait,
        TimeoutSeconds = 30,
        PollIntervalSeconds = 1
    };
}
