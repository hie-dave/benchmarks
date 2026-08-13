namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Stores status for one aggregate evaluation attempt of a benchmark submission.
/// </summary>
public class EvaluationRun
{
    public int Id { get; set; }

    public int BenchmarkSubmissionId { get; set; }

    public EvaluationRunStatus Status { get; set; }

    public bool? Passed { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public BenchmarkSubmission BenchmarkSubmission { get; set; } = null!;
    public ICollection<EvaluationRunDataset> Datasets { get; set; } = new List<EvaluationRunDataset>();
}
