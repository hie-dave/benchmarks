namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents a single data point in space and time
/// </summary>
public record DataPoint(double Longitude, double Latitude, DateTime Timestamp, double Value);
