namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Represents a single data point in time
/// </summary>
public record DataPoint(DateTime Timestamp, double Longitude, double Latitude, double Value);
