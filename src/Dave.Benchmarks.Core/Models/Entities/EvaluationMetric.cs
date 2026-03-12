namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Stores one computed metric value for an evaluation result.
/// </summary>
public class EvaluationMetric
{
    public int Id { get; set; }

    public int EvaluationResultId { get; set; }

    /// <summary>
    /// Stable canonical metric key (for example, "r2", "nse", "rsr", "n").
    /// </summary>
    public string MetricType { get; set; } = string.Empty;

    /// <summary>
    /// Computed metric value.
    /// </summary>
    public double Value { get; set; }

    public EvaluationResult EvaluationResult { get; set; } = null!;
}
