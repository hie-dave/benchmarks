namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a single site-level model run.
/// </summary>
public class SiteRun : Simulation
{
    /// <summary>
    /// The name of the site.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude of the site in degrees.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the site in degrees.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The dataset this site run belongs to.
    /// </summary>
    public int DatasetId { get; set; }

    /// <summary>
    /// Navigation property for the dataset this site run belongs to.
    /// </summary>
    public SiteRunDataset Dataset { get; set; } = null!;
}
