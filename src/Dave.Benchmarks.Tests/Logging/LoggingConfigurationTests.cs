using Dave.Benchmarks.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Dave.Benchmarks.Tests.Logging;

public class LoggingConfigurationTests
{
    [Fact]
    public void ConfigureLogging_ReturnsSameServiceCollection()
    {
        ServiceCollection services = [];

        IServiceCollection configured = services.ConfigureLogging();

        Assert.Same(services, configured);
    }

    [Fact]
    public void ConfigureLogging_ConfiguresConsoleFormatterName()
    {
        ServiceCollection services = [];
        services.ConfigureLogging();
        using ServiceProvider provider = services.BuildServiceProvider();

        ConsoleLoggerOptions options = provider.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;

        Assert.Equal(CustomConsoleFormatterOptions.FormatterName, options.FormatterName);
    }

    [Fact]
    public void ConfigureLogging_ConfiguresCustomFormatterOptions()
    {
        ServiceCollection services = [];
        services.ConfigureLogging();
        using ServiceProvider provider = services.BuildServiceProvider();

        CustomConsoleFormatterOptions options = provider.GetRequiredService<IOptionsMonitor<CustomConsoleFormatterOptions>>().CurrentValue;

        Assert.Equal("HH:mm:ss ", options.TimestampFormat);
        Assert.True(options.IncludeScopes);
    }

    [Fact]
    public void ConfigureLogging_RegistersCustomConsoleFormatter()
    {
        ServiceCollection services = [];
        services.ConfigureLogging();
        using ServiceProvider provider = services.BuildServiceProvider();

        IEnumerable<ConsoleFormatter> formatters = provider.GetServices<ConsoleFormatter>();

        Assert.Contains(formatters, formatter => formatter is CustomConsoleFormatter);
    }

    [Fact]
    public void ConfigureLogging_CreatesLoggerFromFactory()
    {
        ServiceCollection services = [];
        services.ConfigureLogging();
        using ServiceProvider provider = services.BuildServiceProvider();

        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("test-category");

        Assert.NotNull(logger);
    }
}
