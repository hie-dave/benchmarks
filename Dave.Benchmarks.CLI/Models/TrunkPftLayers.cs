namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents layer metadata for trunk output files which contain data for each
/// PFT. In these files, all data layers have the same units.
/// </summary>
public class TrunkPftLayers : ILayerDefinitions
{
    private readonly Unit units;

    public TrunkPftLayers(Unit units)
    {
        this.units = units;
    }

    public Unit GetUnits(string layer)
    {
        if (!IsDataLayer(layer))
            throw new InvalidOperationException($"Layer {layer} is not a data layer");

        // We can't verify that layer is a valid data file - by definition, any
        // column name in the output file, other than the predefined metadata
        // columns, could be the name of a PFT and thus a data layer.

        return units;
    }

    public bool IsDataLayer(string layer)
    {
        return !ModelConstants.TrunkMetadataLayers.Contains(layer);
    }
}
