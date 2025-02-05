namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Represents a data column in an output file
/// </summary>
public class Layer
{
    /// <summary>
    /// Name of the data column.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Units of the data in this column.
    /// </summary>
    public Unit Unit { get; }

    /// <summary>
    /// Data points in this column.
    /// </summary>
    public IReadOnlyList<DataPoint> Data { get; }

    /// <summary>
    /// Create a new layer.
    /// </summary>
    /// <param name="name">Name of the data column.</param>
    /// <param name="unit">Units of the data in this column.</param>
    /// <param name="dataPoints">Data points in this column.</param>
    public Layer(string name, Unit unit, IReadOnlyList<DataPoint> dataPoints)
    {
        Name = name;
        Unit = unit;
        Data = dataPoints;
    }
}
