using LpjGuess.Core.Models.Entities;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Tracks the accepted observation baseline dataset for a simulation and channel scope.
/// </summary>
public class ObservationBaselineRegistryEntry
{
    public int Id { get; set; }

    public string SimulationId { get; set; } = string.Empty;

    public string BaselineChannel { get; set; } = string.Empty;

    public int ObservationDatasetId { get; set; }

    public DateTime AcceptedAt { get; set; }

    public Dataset ObservationDataset { get; set; } = null!;
}
