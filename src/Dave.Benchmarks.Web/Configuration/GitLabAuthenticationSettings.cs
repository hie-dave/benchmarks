using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Configuration;

public class GitLabAuthenticationSettings
{
    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [MinLength(1)]
    public string[] AllowedProjectIds { get; set; } = [];

    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), true);

        if (!Uri.TryCreate(Authority, UriKind.Absolute, out Uri? authority) ||
            authority.Scheme != Uri.UriSchemeHttps)
        {
            throw new ValidationException(
                "Authentication:GitLab:Authority must be an absolute HTTPS URI.");
        }

        Authority = Authority.TrimEnd('/');
        Audience = Audience.Trim();
        AllowedProjectIds = AllowedProjectIds
            .Select(id => id.Trim())
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (AllowedProjectIds.Length == 0)
        {
            throw new ValidationException(
                "Authentication:GitLab:AllowedProjectIds must contain at least one project ID.");
        }
    }
}
