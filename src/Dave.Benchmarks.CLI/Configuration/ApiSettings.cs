using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.CLI.Configuration;

public class ApiSettings
{
    [Required]
    [Url]
    public string WebApiUrl { get; set; } = string.Empty;
    
    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
    }
}
