using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Configuration;

/// <summary>
/// Production trust settings for the configured JWT bearer scheme.
/// </summary>
public class AuthenticationSettings
{
    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string[] ValidAudiences { get; set; } = [];

    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), true);

        Authority = Authority.TrimEnd('/');
        if (!IsAbsoluteHttpsUri(Authority))
        {
            throw new ValidationException(
                "Authentication:Schemes:Bearer:Authority must be an absolute HTTPS URI.");
        }

        ValidAudiences = ValidAudiences
            .Select(audience => audience.Trim())
            .Where(audience => audience.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ValidAudiences.Length == 0)
        {
            throw new ValidationException(
                "Authentication:Schemes:Bearer:ValidAudiences must contain at least one audience.");
        }

        if (ValidAudiences.Any(audience => !IsAbsoluteHttpsUri(audience)))
        {
            throw new ValidationException(
                "Authentication:Schemes:Bearer:ValidAudiences must contain only absolute HTTPS URIs.");
        }
    }

    private static bool IsAbsoluteHttpsUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
