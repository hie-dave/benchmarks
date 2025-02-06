namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents layer metadata for output files which contain data for each
/// individual. In these files, all data layers have the same units.
/// </summary>
public class IndividualLayers : DaveLayers
{
    /// <summary>
    /// Construct an individual-level output file.
    /// </summary>
    /// <param name="layers">A list of (layer name, units) pairs for each layer in the output file.</param>
    public IndividualLayers((string layer, Unit units)[] layers) : base(layers)
    {
    }

    /// <summary>
    /// Construct an individual-level output file in which all layers have the
    /// same units.
    /// </summary>
    /// <param name="layers">A list of layer names.</param>
    /// <param name="units">The units of all layers in the output file.</param>
    public IndividualLayers(IEnumerable<string> layers, Unit units) : base(layers, units)
    {
    }

    /// <inheritdoc />
    public override bool IsDataLayer(string layer)
    {
        return !ModelConstants.DaveIndivMetadataLayers.Contains(layer);
    }
}
