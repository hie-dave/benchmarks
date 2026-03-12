namespace Dave.Benchmarks.Core.Services.Metrics;

/// <summary>
/// Coefficient of determination (R squared).
/// </summary>
public sealed class R2Metric : IMetric
{
    public string Type => "r2";

    public string Name => "R2";

    public string Description => "Coefficient of determination between observed and predicted values.";

    public double? Compute(IReadOnlyList<MetricSeries> series)
    {
        int n = series.Count;
        if (n < 2)
            return null;

        double sumObs = 0;
        double sumPred = 0;
        for (int i = 0; i < n; i++)
        {
            sumObs += series[i].Observed;
            sumPred += series[i].Predicted;
        }

        double meanObs = sumObs / n;
        double meanPred = sumPred / n;

        double cov = 0;
        double varObs = 0;
        double varPred = 0;
        for (int i = 0; i < n; i++)
        {
            double obs = series[i].Observed - meanObs;
            double pred = series[i].Predicted - meanPred;
            cov += obs * pred;
            varObs += obs * obs;
            varPred += pred * pred;
        }

        if (varObs <= 0 || varPred <= 0)
            return null;

        double r = cov / Math.Sqrt(varObs * varPred);
        return r * r;
    }

    public bool IsImprovement(double baselineValue, double candidateValue)
    {
        return candidateValue > baselineValue;
    }
}
