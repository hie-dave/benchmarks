namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Stores per-variable and per-layer evaluation details and metrics for a run.
/// </summary>
public class EvaluationResult
{
    public int Id { get; set; }

    public int EvaluationRunId { get; set; }

    public int CandidateVariableId { get; set; }

    public int CandidateLayerId { get; set; }

    public int? BaselineVariableId { get; set; }

    public int? BaselineLayerId { get; set; }

    public int ObservationVariableId { get; set; }

    public int ObservationLayerId { get; set; }

    public EvaluationRun EvaluationRun { get; set; } = null!;

    public Variable CandidateVariable { get; set; } = null!;

    public VariableLayer CandidateLayer { get; set; } = null!;

    public Variable? BaselineVariable { get; set; }

    public VariableLayer? BaselineLayer { get; set; }

    public Variable ObservationVariable { get; set; } = null!;

    public VariableLayer ObservationLayer { get; set; } = null!;

    public ICollection<EvaluationMetric> Metrics { get; set; } = new List<EvaluationMetric>();
}
