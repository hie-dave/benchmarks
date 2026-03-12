namespace Dave.Benchmarks.Core.Services.Metrics;

/// <summary>
/// Contract for calculating one evaluation metric.
/// </summary>
public interface IMetric
{
    /// <summary>
    /// Stable metric key (for example, "r2", "nse", "rsr", "n").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Human-readable metric name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable metric description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Compute a metric value over paired observed/predicted values.
    /// Returns null if the metric is not defined for the provided series.
    /// </summary>
    double? Compute(IReadOnlyList<MetricSeries> series);

    /// <summary>
    /// Returns true if the candidate metric result is an improvement over baseline.
    /// </summary>
    bool IsImprovement(double baselineValue, double candidateValue);
}
