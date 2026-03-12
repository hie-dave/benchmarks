namespace Dave.Benchmarks.Core.Services.Metrics;

/// <summary>
/// Count of paired observed/predicted values used in metric calculations.
/// </summary>
public sealed class CountMetric : IMetric
{
    public string Type => "n";

    public string Name => "N";

    public string Description => "Number of matched observed/predicted values.";

    public double? Compute(IReadOnlyList<MetricSeries> series)
    {
        return series.Count;
    }

    public bool IsImprovement(double baselineValue, double candidateValue)
    {
        return candidateValue > baselineValue;
    }
}
