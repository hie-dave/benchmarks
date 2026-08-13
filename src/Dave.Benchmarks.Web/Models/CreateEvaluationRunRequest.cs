using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Models;

public class CreateEvaluationRunRequest
{
    [Required]
    public int BenchmarkSubmissionId { get; set; }
}
