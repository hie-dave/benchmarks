using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Configuration;

public class AuthorisationSettings
{
    [MinLength(1)]
    public string[] AllowedGitlabProjectIds { get; set; } = [];

    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), true);

        AllowedGitlabProjectIds = AllowedGitlabProjectIds
            .Select(id => id.Trim())
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (AllowedGitlabProjectIds.Length == 0)
        {
            throw new ValidationException(
                "Authorisation:AllowedGitlabProjectIds must contain at least one project ID.");
        }
    }
}
