namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Stores per-variable and per-layer evaluation details and metrics for a run.
/// </summary>
public class EvaluationResult
{
    public int Id { get; set; }

    public int EvaluationRunId { get; set; }

    public string VariableName { get; set; } = string.Empty;

    public string LayerName { get; set; } = string.Empty;

    public int MatchedPointCount { get; set; }

    public int NumericMismatchCount { get; set; }

    public bool StructuralMismatch { get; set; }

    public double? R2 { get; set; }

    public double? Rsr { get; set; }

    public double? Nse { get; set; }

    public int? N { get; set; }

    public EvaluationRun EvaluationRun { get; set; } = null!;
}
