using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Dave.Benchmarks.CLI.Commands;

namespace Dave.Benchmarks.Tests.CLI;

public class CommandRunnerTests
{
    private class TestHandler
    {
        public bool Ran;
        public Task Do()
        {
            Ran = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RunAsync_Success_ReturnsZero()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestHandler>();
        var provider = services.BuildServiceProvider();

        var runner = new CommandRunner(provider, NullLogger<CommandRunner>.Instance);

        int result = await runner.RunAsync<TestHandler>(h => h.Do());

        Assert.Equal(0, result);
        var handler = provider.GetRequiredService<TestHandler>();
        Assert.True(handler.Ran);
    }

    [Fact]
    public async Task RunAsync_HandlerThrows_ReturnsOne()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestHandler>();
        var provider = services.BuildServiceProvider();

        var runner = new CommandRunner(provider, NullLogger<CommandRunner>.Instance);

        int result = await runner.RunAsync<TestHandler>(_ => throw new InvalidOperationException("boom"));

        Assert.Equal(1, result);
    }
}
