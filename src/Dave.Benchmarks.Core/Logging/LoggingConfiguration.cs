using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.Core.Logging;

/// <summary>
/// Provides centralized logging configuration for both CLI and Web applications.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures logging with a consistent format across all applications.
    /// </summary>
    /// <param name="services">The service collection to configure logging for.</param>
    public static IServiceCollection ConfigureLogging(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole(options =>
            {
                options.FormatterName = CustomConsoleFormatterOptions.FormatterName;
            });
            logging.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>(options =>
            {
                options.TimestampFormat = "HH:mm:ss ";
                options.IncludeScopes = true;
            });
        });

        return services;
    }
}
