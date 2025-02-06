namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents layer metadata for output files which contain multiple layers.
/// </summary>
public abstract class Layers : ILayerDefinitions
{
    /// <summary>
    /// A list of (layer name, units) pairs for each layer in the output file.
    /// </summary>
    private readonly (string layer, Unit units)[] layers;

    /// <summary>
    /// Construct a layers instance with custom layer definitions.
    /// </summary>
    /// <param name="layers">A list of (layer name, units) pairs for each layer in the output file.</param>
    public Layers((string layer, Unit units)[] layers)
    {
        this.layers = layers;
    }

    /// <summary>
    /// Construct a layers instance in which all layers have the same units.
    /// </summary>
    /// <param name="layers">A list of layer names.</param>
    /// <param name="units">The units of all layers in the output file.</param>
    public Layers(IEnumerable<string> layers, Unit units)
    {
        this.layers = [.. layers.Select(l => (l, units))];
    }

    /// <inheritdoc /> 
    public virtual Unit GetUnits(string layer)
    {
        if (!IsDataLayer(layer))
            throw new InvalidOperationException($"Layer {layer} is not a data layer");

        (string _, Unit Units)? l = layers.FirstOrDefault(l => l.layer == layer);
        if (l is null)
            throw new InvalidOperationException($"Layer {layer} is not a data layer");

        return l.Value.Units;
    }

    /// <inheritdoc />
    public abstract bool IsDataLayer(string layer);
}
