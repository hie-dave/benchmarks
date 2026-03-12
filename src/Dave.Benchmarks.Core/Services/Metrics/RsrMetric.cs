namespace Dave.Benchmarks.Core.Services.Metrics;

/// <summary>
/// RSR metric: RMSE divided by the standard deviation of observations.
/// </summary>
public sealed class RsrMetric : IMetric
{
    public string Type => "rsr";

    public string Name => "RSR";

    public string Description => "RMSE normalized by the standard deviation of observations.";

    public double? Compute(IReadOnlyList<MetricSeries> series)
    {
        int n = series.Count;
        if (n < 2)
            return null;

        double mseSum = 0;
        double obsSum = 0;
        for (int i = 0; i < n; i++)
        {
            double diff = series[i].Observed - series[i].Predicted;
            mseSum += diff * diff;
            obsSum += series[i].Observed;
        }

        double rmse = Math.Sqrt(mseSum / n);
        double meanObs = obsSum / n;

        double varSum = 0;
        for (int i = 0; i < n; i++)
        {
            double d = series[i].Observed - meanObs;
            varSum += d * d;
        }

        // Sample standard deviation on observations.
        double stdObs = Math.Sqrt(varSum / (n - 1));
        if (stdObs <= 0)
            return null;

        return rmse / stdObs;
    }

    public bool IsImprovement(double baselineValue, double candidateValue)
    {
        return candidateValue < baselineValue;
    }
}
