namespace Dave.Benchmarks.Core.Services.Metrics;

/// <summary>
/// Paired observed and predicted values used for metric calculation.
/// </summary>
public readonly record struct MetricSeries(double Observed, double Predicted);
