using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Request model for adding a new site run to a dataset.
/// </summary>
public sealed class AddSiteRequest : AddRunRequestBase
{
    /// <summary>
    /// Name of the site.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Latitude of the site in degrees.
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude of the site in degrees.
    /// </summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
    public double Longitude { get; set; }
}
