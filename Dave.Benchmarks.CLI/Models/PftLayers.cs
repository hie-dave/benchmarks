namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents layer metadata for output files which contain data for each PFT.
/// In these files, all data layers have the same units.
/// </summary>
public class PftLayers : ILayerDefinitions
{
    // fixme - refer to constants
    private static readonly string[] ignoredLayers = { "Lon", "Lat", "Year", "Day", "patch", "stand" };
    private readonly Unit units;

    public PftLayers(Unit units)
    {
        this.units = units;
    }

    public Unit GetUnits(string layer)
    {
        if (!IsDataLayer(layer))
            throw new InvalidOperationException($"Layer {layer} is not a data layer");

        return units;
    }

    public bool IsDataLayer(string layer)
    {
        if (ignoredLayers.Contains(layer))
            return false;

        return true;
    }
}
