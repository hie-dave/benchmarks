namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents layer metadata for trunk output files.
/// </summary>
public class TrunkLayers : Layers
{
    /// <summary>
    /// Construct a trunk layers instance with custom layer definitions.
    /// </summary>
    /// <param name="layers">A list of (layer name, units) pairs for each layer in the output file.</param>
    public TrunkLayers((string layer, Unit units)[] layers) : base(layers)
    {
    }

    /// <summary>
    /// Construct a layers instance in which all layers have the same units.
    /// </summary>
    /// <param name="layers">A list of layer names.</param>
    /// <param name="units">The units of all layers in the output file.</param>
    public TrunkLayers(IEnumerable<string> layers, Unit units) : base(layers, units)
    {
    }

    /// <inheritdoc />
    public override bool IsDataLayer(string layer)
    {
        return !ModelConstants.TrunkMetadataLayers.Contains(layer);
    }
}
