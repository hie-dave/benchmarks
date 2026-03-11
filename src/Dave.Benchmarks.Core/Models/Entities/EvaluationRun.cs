using LpjGuess.Core.Models.Entities;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Stores metadata and status for a single candidate-vs-baseline evaluation run.
/// </summary>
public class EvaluationRun
{
    public int Id { get; set; }

    public string SimulationId { get; set; } = string.Empty;

    public string BaselineChannel { get; set; } = string.Empty;

    public int CandidatePredictionDatasetId { get; set; }

    public int? BaselinePredictionDatasetId { get; set; }

    public int? ObservationBaselineDatasetId { get; set; }

    public string MergeRequestId { get; set; } = string.Empty;

    public string SourceBranch { get; set; } = string.Empty;

    public string TargetBranch { get; set; } = string.Empty;

    public string CommitSha { get; set; } = string.Empty;

    public EvaluationRunStatus Status { get; set; }

    public bool? Passed { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public Dataset CandidatePredictionDataset { get; set; } = null!;

    public Dataset? BaselinePredictionDataset { get; set; }

    public Dataset? ObservationBaselineDataset { get; set; }

    public ICollection<EvaluationResult> Results { get; set; } = new List<EvaluationResult>();
}
