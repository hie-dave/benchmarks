namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a dataset containing observations.
/// </summary>
public class ObservationDataset : Dataset
{
    // Required for reproducibility
    public string Source { get; set; } = string.Empty;  // Source of the observation data
    public string Version { get; set; } = string.Empty; // Version of the dataset
}
