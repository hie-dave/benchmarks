using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Commands;

public class CommandRunner
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CommandRunner> _logger;

    public CommandRunner(IServiceProvider services, ILogger<CommandRunner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<int> RunAsync<T>(Func<T, Task> action) where T : notnull
    {
        try
        {
            // Get the handler from the DI container instead of creating it manually
            T handler = _services.GetRequiredService<T>();
            await action(handler);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command failed");
            return 1;
        }
    }
}
