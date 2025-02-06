using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Dave.Benchmarks.CLI.Logging;

public class CustomConsoleFormatter : ConsoleFormatter
{
    private readonly IOptionsMonitor<CustomConsoleFormatterOptions> _options;

    public CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> options)
        : base(CustomConsoleFormatterOptions.FormatterName)
    {
        _options = options;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message == null)
            return;

        CustomConsoleFormatterOptions options = _options.CurrentValue;
        string timestamp = DateTime.Now.ToString(options.TimestampFormat);
        textWriter.Write($"{timestamp}{GetLogLevelString(logEntry.LogLevel)}:");

        if (options.IncludeScopes && scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, writer) =>
            {
                writer.Write($" [{scope}]");
            }, textWriter);
        }

        textWriter.Write($" {message}");
        
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine();
            textWriter.WriteLine(logEntry.Exception.ToString());
        }
        else
        {
            textWriter.WriteLine();
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "????"
        };
    }
}

public class CustomConsoleFormatterOptions : ConsoleFormatterOptions
{
    public const string FormatterName = "Custom";
}
