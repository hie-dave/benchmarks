using System.ComponentModel.DataAnnotations;
using Dave.Benchmarks.Web.Configuration;

namespace Dave.Benchmarks.Tests.Configuration;

public class GitLabOAuthSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_Succeeds()
    {
        ValidSettings().Validate();
    }

    [Fact]
    public void Validate_WithHttpIssuer_Throws()
    {
        GitLabOAuthSettings settings = ValidSettings();
        settings.TokenIssuer = "http://benchmarks.example.test";
        Assert.Throws<ValidationException>(settings.Validate);
    }

    [Fact]
    public void Validate_WithShortSigningKey_Throws()
    {
        GitLabOAuthSettings settings = ValidSettings();
        settings.SigningKey = Convert.ToBase64String(new byte[16]);
        Assert.Throws<ValidationException>(settings.Validate);
    }

    private static GitLabOAuthSettings ValidSettings() => new()
    {
        TokenIssuer = "https://benchmarks.example.test",
        SigningKey = Convert.ToBase64String(new byte[32]),
        TokenLifetimeMinutes = 60
    };
}
