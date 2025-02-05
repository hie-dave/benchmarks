namespace Dave.Benchmarks.Core.Models;

public class ObservationDataset : Dataset
{
    public string DataSource { get; set; } = string.Empty;          // e.g., "OzFlux Network"
    public string CollectionMethod { get; set; } = string.Empty;    // e.g., "Eddy Covariance"
    public string? QualityControlNotes { get; set; }               // Any QC information
}
