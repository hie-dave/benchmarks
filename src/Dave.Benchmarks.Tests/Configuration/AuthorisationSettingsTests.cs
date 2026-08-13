using Dave.Benchmarks.Web.Configuration;

namespace Dave.Benchmarks.Tests.Configuration;

public class AuthorisationSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_NormalisesValues()
    {
        AuthorisationSettings settings = new()
        {
            AllowedGitlabProjectIds = [" 123 ", "123", "456"]
        };

        settings.Validate();

        Assert.Equal(["123", "456"], settings.AllowedGitlabProjectIds);
    }
}
