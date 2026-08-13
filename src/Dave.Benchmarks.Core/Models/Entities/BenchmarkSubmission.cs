namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// A coherent set of prediction datasets produced by one CI benchmark
/// execution for one tested commit.
/// </summary>
public class BenchmarkSubmission
{
    public int Id { get; set; }
    public string GitLabProjectId { get; set; } = string.Empty;
    public string MergeRequestId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public string? CommitMessage { get; set; }
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string BenchmarkName { get; set; } = string.Empty;
    public BenchmarkSubmissionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public ICollection<PredictionDataset> Datasets { get; set; } = new List<PredictionDataset>();
    public ICollection<EvaluationRun> EvaluationRuns { get; set; } = new List<EvaluationRun>();
}
