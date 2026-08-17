using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Models;

public class ExchangeGitLabTokenRequest
{
    [Required] public string AccessToken { get; set; } = string.Empty;
}
