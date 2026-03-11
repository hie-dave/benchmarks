using LpjGuess.Core.Models.Entities;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Tracks the accepted prediction baseline dataset for a simulation and channel scope.
/// </summary>
public class PredictionBaselineRegistryEntry
{
    public int Id { get; set; }

    public string SimulationId { get; set; } = string.Empty;

    public string BaselineChannel { get; set; } = string.Empty;

    public int PredictionDatasetId { get; set; }

    public DateTime AcceptedAt { get; set; }

    public Dataset PredictionDataset { get; set; } = null!;
}
