using Dave.Benchmarks.Web.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Tests.Configuration;

public class ConnectionStringsSettingsTests
{
    [Fact]
    public void Validate_WithValidConnectionString_DoesNotThrow()
    {
        ConnectionStringsSettings settings = new()
        {
            DefaultConnection = "Server=localhost;Database=test;User Id=user;Password=pass;"
        };

        settings.Validate();
    }

    [Fact]
    public void Validate_WithMissingDefaultConnection_ThrowsValidationException()
    {
        ConnectionStringsSettings settings = new()
        {
            DefaultConnection = string.Empty
        };

        Assert.Throws<ValidationException>(() => settings.Validate());
    }

    [Fact]
    public void Validate_WithMalformedConnectionString_ThrowsValidationException()
    {
        ConnectionStringsSettings settings = new()
        {
            DefaultConnection = "Data Source=\"unterminated"
        };

        ValidationException ex = Assert.Throws<ValidationException>(() => settings.Validate());
        Assert.Contains("valid connection string", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
