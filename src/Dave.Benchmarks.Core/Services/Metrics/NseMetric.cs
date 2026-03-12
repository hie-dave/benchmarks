namespace Dave.Benchmarks.Core.Services.Metrics;

/// <summary>
/// Nash-Sutcliffe model efficiency coefficient.
/// </summary>
public sealed class NseMetric : IMetric
{
    public string Type => "nse";

    public string Name => "NSE";

    public string Description => "Nash-Sutcliffe model efficiency coefficient.";

    public double? Compute(IReadOnlyList<MetricSeries> series)
    {
        int n = series.Count;
        if (n == 0)
            return null;

        double obsSum = 0;
        for (int i = 0; i < n; i++)
            obsSum += series[i].Observed;

        double meanObs = obsSum / n;

        double numerator = 0;
        double denominator = 0;
        for (int i = 0; i < n; i++)
        {
            double obs = series[i].Observed;
            double pred = series[i].Predicted;
            double e = obs - pred;
            numerator += e * e;

            double d = obs - meanObs;
            denominator += d * d;
        }

        if (denominator <= 0)
            return null;

        return 1.0 - (numerator / denominator);
    }

    public bool IsImprovement(double baselineValue, double candidateValue)
    {
        return candidateValue > baselineValue;
    }
}
