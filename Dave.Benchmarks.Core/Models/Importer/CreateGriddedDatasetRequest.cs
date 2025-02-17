using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Request model for creating a new gridded dataset.
/// </summary>
public sealed class CreateGriddedDatasetRequest : CreateDatasetRequestBase
{
    /// <summary>
    /// Spatial resolution of the gridded data (e.g., "0.5deg").
    /// </summary>
    [Required]
    public string SpatialResolution { get; set; } = string.Empty;

    /// <summary>
    /// Spatial extent of the gridded data (e.g., "global", "europe").
    /// </summary>
    [Required]
    public string SpatialExtent { get; set; } = string.Empty;
}
