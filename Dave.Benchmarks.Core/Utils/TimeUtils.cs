namespace Dave.Benchmarks.Core.Utils;

public static class TimeUtils
{
    /// <summary>
    /// Formats a TimeSpan into a human-readable string.
    /// </summary>
    /// <param name="span">The TimeSpan to format.</param>
    /// <returns>A human-readable string representation of the TimeSpan.</returns>
    public static string FormatTimeSpan(TimeSpan span)
    {
        if (span.TotalSeconds < 60)
            return $"{span.TotalSeconds:F1} seconds";
        
        if (span.TotalMinutes < 60)
            return $"{span.TotalMinutes:F1} minutes";
        
        if (span.TotalHours < 24)
            return $"{span.TotalHours:F1} hours";
        
        if (span.TotalDays < 30)
            return $"{span.TotalDays:F1} days";
        
        if (span.TotalDays < 365)
        {
            double months = span.TotalDays / 30;
            return $"{months:F1} months";
        }
        
        double years = span.TotalDays / 365;
        return $"{years:F1} years";
    }
}
