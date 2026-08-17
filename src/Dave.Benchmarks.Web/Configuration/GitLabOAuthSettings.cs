using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Configuration;

public class GitLabOAuthSettings
{
    [Required] public string TokenIssuer { get; set; } = string.Empty;
    [Required] public string SigningKey { get; set; } = string.Empty;
    [Range(5, 120)] public int TokenLifetimeMinutes { get; set; } = 60;

    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), true);
        if (!Uri.TryCreate(TokenIssuer, UriKind.Absolute, out Uri? issuer) || issuer.Scheme != Uri.UriSchemeHttps)
            throw new ValidationException("GitLabOAuth:TokenIssuer must be an absolute HTTPS URI.");
        try
        {
            if (Convert.FromBase64String(SigningKey).Length >= 32) return;
        }
        catch (FormatException)
        {
        }
        throw new ValidationException("GitLabOAuth:SigningKey must be base64-encoded and at least 32 bytes.");
    }
}
