namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>Snapshot and outcome of one dataset within an evaluation run.</summary>
public class EvaluationRunDataset
{
    public int Id { get; set; }
    public int EvaluationRunId { get; set; }
    public int CandidateDatasetId { get; set; }
    public int? BaselineDatasetId { get; set; }
    public EvaluationRunStatus Status { get; set; }
    public bool? Passed { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public EvaluationRun EvaluationRun { get; set; } = null!;
    public PredictionDataset CandidateDataset { get; set; } = null!;
    public PredictionDataset? BaselineDataset { get; set; }
    public ICollection<EvaluationResult> Results { get; set; } = new List<EvaluationResult>();
}
