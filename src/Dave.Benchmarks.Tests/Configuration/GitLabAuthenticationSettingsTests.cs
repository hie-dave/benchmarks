using Dave.Benchmarks.Web.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Tests.Configuration;

public class GitLabAuthenticationSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_NormalisesValues()
    {
        GitLabAuthenticationSettings settings = new()
        {
            Authority = "https://gitlab.example.com/",
            Audience = " https://benchmarks.example.com ",
            AllowedProjectIds = [" 123 ", "123", "456"]
        };

        settings.Validate();

        Assert.Equal("https://gitlab.example.com", settings.Authority);
        Assert.Equal("https://benchmarks.example.com", settings.Audience);
        Assert.Equal(["123", "456"], settings.AllowedProjectIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("gitlab.example.com")]
    [InlineData("http://gitlab.example.com")]
    public void Validate_WithInvalidAuthority_Throws(string authority)
    {
        GitLabAuthenticationSettings settings = ValidSettings();
        settings.Authority = authority;

        Assert.Throws<ValidationException>(() => settings.Validate());
    }

    [Fact]
    public void Validate_WithoutAllowedProjects_Throws()
    {
        GitLabAuthenticationSettings settings = ValidSettings();
        settings.AllowedProjectIds = [];

        Assert.Throws<ValidationException>(() => settings.Validate());
    }

    private static GitLabAuthenticationSettings ValidSettings() => new()
    {
        Authority = "https://gitlab.example.com",
        Audience = "https://benchmarks.example.com",
        AllowedProjectIds = ["123"]
    };
}
