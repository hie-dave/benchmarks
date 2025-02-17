namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a gridded model run, potentially containing multiple climate scenarios.
/// </summary>
public class GriddedDataset : PredictionDataset
{
    /// <summary>
    /// The spatial extent of the gridded data.
    /// </summary>
    public string SpatialExtent { get; set; } = string.Empty;

    /// <summary>
    /// The climate scenarios that make up this dataset.
    /// </summary>
    public ICollection<ClimateScenario> ClimateScenarios { get; set; } = 
        new List<ClimateScenario>();
}
