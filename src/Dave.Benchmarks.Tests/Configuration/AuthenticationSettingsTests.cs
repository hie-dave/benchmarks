using Dave.Benchmarks.Web.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Tests.Configuration;

public class AuthenticationSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_NormalisesValues()
    {
        AuthenticationSettings settings = new()
        {
            Authority = "https://gitlab.example.com/",
            ValidAudiences = [" https://benchmarks.example.com ", "https://benchmarks.example.com"]
        };

        settings.Validate();

        Assert.Equal("https://gitlab.example.com", settings.Authority);
        Assert.Equal(["https://benchmarks.example.com"], settings.ValidAudiences);
    }

    [Theory]
    [InlineData("")]
    [InlineData("gitlab.example.com")]
    [InlineData("http://gitlab.example.com")]
    public void Validate_WithInvalidAuthority_Throws(string authority)
    {
        AuthenticationSettings settings = ValidSettings();
        settings.Authority = authority;

        ValidationException exception = Assert.Throws<ValidationException>(() => settings.Validate());
        Assert.Contains("Authority", exception.Message);
    }

    [Fact]
    public void Validate_WithoutAudiences_Throws()
    {
        AuthenticationSettings settings = ValidSettings();
        settings.ValidAudiences = [];

        ValidationException exception = Assert.Throws<ValidationException>(() => settings.Validate());
        Assert.Contains("ValidAudiences", exception.Message);
    }

    [Fact]
    public void Validate_WithNullAudiences_Throws()
    {
        AuthenticationSettings settings = ValidSettings();
        settings.ValidAudiences = null!;

        ValidationException exception = Assert.Throws<ValidationException>(() => settings.Validate());
        Assert.Contains("ValidAudiences", exception.Message);
    }

    [Theory]
    [InlineData("benchmarks.example.com")]
    [InlineData("http://benchmarks.example.com")]
    public void Validate_WithInvalidAudience_Throws(string audience)
    {
        AuthenticationSettings settings = ValidSettings();
        settings.ValidAudiences = [audience];

        ValidationException exception = Assert.Throws<ValidationException>(() => settings.Validate());
        Assert.Contains("ValidAudiences", exception.Message);
    }

    private static AuthenticationSettings ValidSettings() => new()
    {
        Authority = "https://gitlab.example.com",
        ValidAudiences = ["https://benchmarks.example.com"]
    };
}
