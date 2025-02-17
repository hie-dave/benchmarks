using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Request model for adding a new gridded run with a specific climate scenario.
/// </summary>
public sealed class AddGriddedRunRequest : AddRunRequestBase
{
    /// <summary>
    /// Name of the Global Climate Model used.
    /// </summary>
    [Required]
    public string GcmName { get; set; } = string.Empty;

    /// <summary>
    /// Emissions scenario used (e.g., SSP1-2.6).
    /// </summary>
    [Required]
    public string EmissionsScenario { get; set; } = string.Empty;
}
