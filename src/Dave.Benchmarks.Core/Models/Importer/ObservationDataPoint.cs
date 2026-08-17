namespace Dave.Benchmarks.Core.Models.Importer;

public record ObservationDataPoint(
    DateTime Timestamp,
    double Value,
    double? Longitude = null,
    double? Latitude = null,
    int? Stand = null,
    int? Patch = null,
    int? Individual = null);

public class AppendObservationDataRequest
{
    public IReadOnlyList<ObservationDataPoint> DataPoints { get; set; } = [];
}
