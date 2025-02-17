namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a specific climate scenario within a gridded dataset.
/// </summary>
public class ClimateScenario : Simulation
{
    /// <summary>
    /// The name of the Global Climate Model used.
    /// </summary>
    public string GcmName { get; set; } = string.Empty;

    /// <summary>
    /// The emissions scenario used (e.g., SSP1-2.6).
    /// </summary>
    public string EmissionsScenario { get; set; } = string.Empty;

    /// <summary>
    /// The dataset this climate scenario belongs to.
    /// </summary>
    public int DatasetId { get; set; }

    /// <summary>
    /// Navigation property for the dataset this climate scenario belongs to.
    /// </summary>
    public GriddedDataset Dataset { get; set; } = null!;
}
