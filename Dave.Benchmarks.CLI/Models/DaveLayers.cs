namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents layer metadata for dave output files.
/// </summary>
public class DaveLayers : Layers
{
    /// <summary>
    /// Construct a layers instance with custom layer definitions.
    /// </summary>
    /// <param name="layers">A list of (layer name, units) pairs for each layer in the output file.</param>
    public DaveLayers((string layer, Unit units)[] layers) : base(layers)
    {
    }

    /// <summary>
    /// Construct a layers instance in which all layers have the same units.
    /// </summary>
    /// <param name="layers">A list of layer names.</param>
    /// <param name="units">The units of all layers in the output file.</param>
    public DaveLayers(IEnumerable<string> layers, Unit units) : base(layers, units)
    {
    }

    /// <inheritdoc />
    public override bool IsDataLayer(string layer)
    {
        return !ModelConstants.DaveMetadataLayers.Contains(layer);
    }
}
