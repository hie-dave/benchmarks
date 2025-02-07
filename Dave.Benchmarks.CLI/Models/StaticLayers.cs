namespace Dave.Benchmarks.CLI.Models;

using Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Layer metadata for output files with a static set of layers known at compile-time.
/// </summary>
public class StaticLayers : ILayerDefinitions
{
    private readonly (string layer, Unit units)[] layers;
    private readonly AggregationLevel level;
    private readonly TemporalResolution resolution;

    /// <summary>
    /// Create a new static layers instance.
    /// </summary>
    /// <param name="layers">A list of (layer name, units) pairs for each layer in the output file.</param>
    /// <param name="level">The level at which data is aggregated.</param>
    /// <param name="resolution">The temporal resolution of the data.</param>
    public StaticLayers((string layer, Unit units)[] layers, AggregationLevel level, TemporalResolution resolution)
    {
        this.layers = layers;
        this.level = level;
        this.resolution = resolution;
    }

    /// <summary>
    /// Create a new static layers instance where all layers have the same units.
    /// </summary>
    /// <param name="layers">A list of layer names.</param>
    /// <param name="units">The units for all layers.</param>
    /// <param name="level">The level at which data is aggregated.</param>
    /// <param name="resolution">The temporal resolution of the data.</param>
    public StaticLayers(IEnumerable<string> layers, Unit units, AggregationLevel level, TemporalResolution resolution)
        : this([.. layers.Select(l => (l, units))], level, resolution)
    {
    }

    /// <inheritdoc />
    public Unit GetUnits(string layer)
    {
        if (!IsDataLayer(layer))
            throw new InvalidOperationException($"Layer {layer} is not a data layer");

        (string _, Unit Units)? l = layers.FirstOrDefault(l => l.layer == layer);
        if (l is null)
            throw new InvalidOperationException($"Layer {layer} is not a data layer");

        return l.Value.Units;
    }

    /// <inheritdoc />
    public bool IsDataLayer(string layer)
    {
        return !ModelConstants.GetMetadataLayers(level, resolution).Contains(layer);
    }
}
