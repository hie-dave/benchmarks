using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.CLI.Configuration;

public class ApiSettings
{
    public const string TokenEnvironmentVariable = "DAVE_BENCHMARKS_TOKEN";

    [Required]
    [Url]
    public string WebApiUrl { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string GitLabUrl { get; set; } = string.Empty;

    public string GitLabOAuthClientId { get; set; } = string.Empty;
    
    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
    }
}
