using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Models;

public class CreateBenchmarkSubmissionRequest
{
    [Required, StringLength(128)] public string MergeRequestId { get; set; } = string.Empty;
    [Required, StringLength(128)] public string PipelineId { get; set; } = string.Empty;
    [Required, StringLength(64)] public string CommitSha { get; set; } = string.Empty;
    [StringLength(1024)] public string? CommitMessage { get; set; }
    [Required, StringLength(256)] public string SourceBranch { get; set; } = string.Empty;
    [Required, StringLength(256)] public string TargetBranch { get; set; } = string.Empty;
    [Required, StringLength(128)] public string BenchmarkName { get; set; } = string.Empty;
}
