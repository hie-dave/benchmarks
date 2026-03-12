using Dave.Benchmarks.Core.Services.Metrics;

namespace Dave.Benchmarks.Tests.Services;

public class MetricsTests
{
    [Fact]
    public void CountMetric_ReturnsSeriesCount()
    {
        CountMetric metric = new();
        double? result = metric.Compute([new MetricSeries(1, 1), new MetricSeries(2, 2)]);
        Assert.Equal(2, result);
    }

    [Fact]
    public void R2Metric_PerfectCorrelation_ReturnsOne()
    {
        R2Metric metric = new();
        double? result = metric.Compute([
            new MetricSeries(1, 1),
            new MetricSeries(2, 2),
            new MetricSeries(3, 3)
        ]);
        Assert.NotNull(result);
        Assert.Equal(1.0, result!.Value, 6);
    }

    [Fact]
    public void R2Metric_ZeroVariance_ReturnsNull()
    {
        R2Metric metric = new();
        double? result = metric.Compute([
            new MetricSeries(1, 1),
            new MetricSeries(1, 2)
        ]);
        Assert.Null(result);
    }

    [Fact]
    public void RsrMetric_PerfectMatch_ReturnsZero()
    {
        RsrMetric metric = new();
        double? result = metric.Compute([
            new MetricSeries(1, 1),
            new MetricSeries(2, 2),
            new MetricSeries(3, 3)
        ]);
        Assert.NotNull(result);
        Assert.Equal(0.0, result!.Value, 6);
    }

    [Fact]
    public void RsrMetric_InsufficientPoints_ReturnsNull()
    {
        RsrMetric metric = new();
        double? result = metric.Compute([new MetricSeries(1, 1)]);
        Assert.Null(result);
    }

    [Fact]
    public void NseMetric_PerfectMatch_ReturnsOne()
    {
        NseMetric metric = new();
        double? result = metric.Compute([
            new MetricSeries(1, 1),
            new MetricSeries(2, 2),
            new MetricSeries(3, 3)
        ]);
        Assert.NotNull(result);
        Assert.Equal(1.0, result!.Value, 6);
    }

    [Fact]
    public void NseMetric_ZeroVarianceObserved_ReturnsNull()
    {
        NseMetric metric = new();
        double? result = metric.Compute([
            new MetricSeries(1, 0),
            new MetricSeries(1, 1)
        ]);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(typeof(R2Metric), 0.2, 0.3, true)]
    [InlineData(typeof(R2Metric), 0.2, 0.1, false)]
    [InlineData(typeof(NseMetric), 0.0, 0.2, true)]
    [InlineData(typeof(NseMetric), 0.4, 0.1, false)]
    [InlineData(typeof(RsrMetric), 1.2, 0.8, true)]
    [InlineData(typeof(RsrMetric), 0.8, 1.2, false)]
    [InlineData(typeof(CountMetric), 10, 11, true)]
    [InlineData(typeof(CountMetric), 10, 9, false)]
    public void IsImprovement_UsesMetricSpecificDirection(Type metricType, double baseline, double candidate, bool expected)
    {
        IMetric metric = (IMetric)Activator.CreateInstance(metricType)!;
        Assert.Equal(expected, metric.IsImprovement(baseline, candidate));
    }

    [Fact]
    public void BuiltInMetrics_ContainsExpectedMetricTypes()
    {
        string[] types = BuiltInMetrics.All.Select(m => m.Type).OrderBy(t => t).ToArray();
        Assert.Equal(["n", "nse", "r2", "rsr"], types);
    }

    [Fact]
    public void BuiltInMetrics_IsKnownType_ValidatesKnownAndUnknown()
    {
        Assert.True(BuiltInMetrics.IsKnownType("r2"));
        Assert.True(BuiltInMetrics.IsKnownType("NSE"));
        Assert.False(BuiltInMetrics.IsKnownType(""));
        Assert.False(BuiltInMetrics.IsKnownType("foo"));
    }
}
