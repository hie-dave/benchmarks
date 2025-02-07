using Dave.Benchmarks.Core.Models.Entities;

namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Represents a physical quantity that can be measured or simulated, such as
/// LAI.
/// </summary>
public class Quantity
{
    /// <summary>
    /// Name of the quantity.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Description of the quantity.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Data layers for the quantity.
    /// </summary>
    public IReadOnlyList<Layer> Layers { get; }

    /// <summary>
    /// The level at which this quantity is aggregated.
    /// </summary>
    public AggregationLevel Level { get; }

    /// <summary>
    /// The temporal resolution of this quantity.
    /// </summary>
    public TemporalResolution Resolution { get; }

    /// <summary>
    /// Create a new quantity.
    /// </summary>
    /// <param name="name">Name of the quantity.</param>
    /// <param name="description">Description of the quantity.</param>
    /// <param name="layers">Data layers for the quantity.</param>
    /// <param name="level">Level at which this quantity is aggregated.</param>
    /// <param name="resolution">Temporal resolution of this quantity.</param>
    public Quantity(
        string name,
        string description,
        IReadOnlyList<Layer> layers,
        AggregationLevel level,
        TemporalResolution resolution)
    {
        Name = name;
        Description = description;
        Layers = layers;
        Level = level;
        Resolution = resolution;
    }

    /// <summary>
    /// Get the layer with the specified name.
    /// </summary>
    /// <param name="name">Name of the layer to get.</param>
    /// <returns>The layer with the specified name, or null if not found.</returns>
    public Layer? GetLayer(string name) => Layers.FirstOrDefault(l => l.Name == name);
}
