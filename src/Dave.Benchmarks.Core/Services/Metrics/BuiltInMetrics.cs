namespace Dave.Benchmarks.Core.Services.Metrics;

/// <summary>
/// Built-in metrics configured for evaluation.
/// </summary>
public static class BuiltInMetrics
{
    public static IReadOnlyList<IMetric> All { get; } =
    [
        new R2Metric(),
        new RsrMetric(),
        new NseMetric(),
        new CountMetric()
    ];

    public static IReadOnlySet<string> KnownTypes { get; } =
        new HashSet<string>(All.Select(m => m.Type), StringComparer.OrdinalIgnoreCase);

    public static bool IsKnownType(string metricType)
    {
        return !string.IsNullOrWhiteSpace(metricType) && KnownTypes.Contains(metricType);
    }

    public static IMetric GetByType(string metricType)
    {
        return All.FirstOrDefault(m => m.Type.Equals(metricType, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown metric type: {metricType}", nameof(metricType));
    }
}
