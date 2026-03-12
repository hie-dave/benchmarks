using System.ComponentModel.DataAnnotations;
using LpjGuess.Core.Models.Entities;

namespace Dave.Benchmarks.Web.Models;

public class CreateObservationDatasetRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Source { get; set; } = string.Empty;

    [Required]
    public string Version { get; set; } = string.Empty;

    public string SpatialResolution { get; set; } = string.Empty;

    [Required]
    public string TemporalResolution { get; set; } = string.Empty;

    public string SimulationId { get; set; } = string.Empty;

    public string Metadata { get; set; } = "{}";

    public int? GroupId { get; set; }

    public MatchingStrategy Strategy { get; set; } = MatchingStrategy.Nearest;

    /// <summary>
    /// Maximum distance (in km) for matching datapoints when using the
    /// Nearest strategy.
    /// </summary>
    public int? MaxDistance { get; set; }
}
