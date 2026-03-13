using Dave.Benchmarks.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Dave.Benchmarks.Tests.Logging;

public class CustomConsoleFormatterTests
{
    [Fact]
    public void Write_FormatterIsNull_WritesNothing()
    {
        CustomConsoleFormatter formatter = CreateFormatter();
        using StringWriter writer = new();

        LogEntry<string> entry = new(
            LogLevel.Information,
            "TestCategory",
            new EventId(1, "evt"),
            "ignored",
            null,
            formatter: null!);

        formatter.Write(in entry, scopeProvider: null, writer);

        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void Write_FormatterReturnsNull_WritesNothing()
    {
        CustomConsoleFormatter formatter = CreateFormatter();
        using StringWriter writer = new();

        LogEntry<string> entry = new(
            LogLevel.Information,
            "TestCategory",
            new EventId(1, "evt"),
            "ignored",
            null,
            formatter: (_, _) => null!);

        formatter.Write(in entry, scopeProvider: null, writer);

        Assert.Equal(string.Empty, writer.ToString());
    }

    [Theory]
    [InlineData(LogLevel.Trace, "trce")]
    [InlineData(LogLevel.Debug, "dbug")]
    [InlineData(LogLevel.Information, "info")]
    [InlineData(LogLevel.Warning, "warn")]
    [InlineData(LogLevel.Error, "fail")]
    [InlineData(LogLevel.Critical, "crit")]
    [InlineData(LogLevel.None, "????")]
    public void Write_WritesExpectedLevelPrefix(LogLevel level, string expectedLevel)
    {
        CustomConsoleFormatter formatter = CreateFormatter();
        using StringWriter writer = new();

        LogEntry<string> entry = new(
            level,
            "TestCategory",
            new EventId(1, "evt"),
            "hello",
            null,
            formatter: static (state, _) => state);

        formatter.Write(in entry, scopeProvider: null, writer);

        string output = writer.ToString();
        Assert.Contains($"{expectedLevel}: hello", output);
    }

    [Fact]
    public void Write_IncludeScopesEnabled_WritesScopes()
    {
        CustomConsoleFormatter formatter = CreateFormatter(includeScopes: true);
        LoggerExternalScopeProvider scopeProvider = new();
        using IDisposable scope1 = scopeProvider.Push("scope-a");
        using IDisposable scope2 = scopeProvider.Push(42);
        using StringWriter writer = new();

        LogEntry<string> entry = new(
            LogLevel.Information,
            "TestCategory",
            new EventId(1, "evt"),
            "hello",
            null,
            formatter: static (state, _) => state);

        formatter.Write(in entry, scopeProvider, writer);

        string output = writer.ToString();
        Assert.Contains(" [scope-a]", output);
        Assert.Contains(" [42]", output);
        Assert.Contains(" hello", output);
    }

    [Fact]
    public void Write_IncludeScopesDisabled_DoesNotWriteScopes()
    {
        CustomConsoleFormatter formatter = CreateFormatter(includeScopes: false);
        LoggerExternalScopeProvider scopeProvider = new();
        using IDisposable scope = scopeProvider.Push("scope-a");
        using StringWriter writer = new();

        LogEntry<string> entry = new(
            LogLevel.Information,
            "TestCategory",
            new EventId(1, "evt"),
            "hello",
            null,
            formatter: static (state, _) => state);

        formatter.Write(in entry, scopeProvider, writer);

        string output = writer.ToString();
        Assert.DoesNotContain("[scope-a]", output);
        Assert.Contains("hello", output);
    }

    [Fact]
    public void Write_ExceptionPresent_WritesExceptionOnNextLine()
    {
        CustomConsoleFormatter formatter = CreateFormatter();
        using StringWriter writer = new();
        InvalidOperationException ex = new("boom");

        LogEntry<string> entry = new(
            LogLevel.Error,
            "TestCategory",
            new EventId(1, "evt"),
            "failed",
            ex,
            formatter: static (state, _) => state);

        formatter.Write(in entry, scopeProvider: null, writer);

        string output = writer.ToString();
        Assert.Contains("fail: failed", output);
        Assert.Contains(ex.ToString(), output);
    }

    [Fact]
    public void Write_UsesConfiguredTimestampFormat()
    {
        CustomConsoleFormatter formatter = CreateFormatter(timestampFormat: "yyyy ");
        using StringWriter writer = new();

        LogEntry<string> entry = new(
            LogLevel.Information,
            "TestCategory",
            new EventId(1, "evt"),
            "hello",
            null,
            formatter: static (state, _) => state);

        formatter.Write(in entry, scopeProvider: null, writer);

        string output = writer.ToString();
        Assert.Matches("^\\d{4} info: hello", output);
    }

    private static CustomConsoleFormatter CreateFormatter(bool includeScopes = false, string timestampFormat = "HH:mm:ss ")
    {
        Mock<IOptionsMonitor<CustomConsoleFormatterOptions>> options = new();
        options.SetupGet(x => x.CurrentValue).Returns(new CustomConsoleFormatterOptions
        {
            IncludeScopes = includeScopes,
            TimestampFormat = timestampFormat
        });

        return new CustomConsoleFormatter(options.Object);
    }
}
