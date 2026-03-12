using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Models;

public class CreateEvaluationRunRequest
{
    [Required]
    public int CandidateDatasetId { get; set; }

    [Required]
    [StringLength(128)]
    public string MergeRequestId { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string SourceBranch { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string TargetBranch { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string CommitSha { get; set; } = string.Empty;
}
