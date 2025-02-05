using System;
using System.Collections.Generic;

namespace Dave.Benchmarks.Core.Models;

/// <summary>
/// Represents a physical quantity that can be measured or simulated, such as LAI or Carbon Mass
/// </summary>
public class Quantity
{
    private readonly Dictionary<string, Layer> _layers = new();

    public string Name { get; }
    public string Description { get; }
    public Unit DefaultUnit { get; }

    public Quantity(string name, string description, Unit defaultUnit)
    {
        Name = name;
        Description = description;
        DefaultUnit = defaultUnit;
    }

    public void AddLayer(string name, IReadOnlyList<DataPoint> dataPoints)
    {
        _layers[name] = new Layer(name, DefaultUnit, dataPoints);
    }

    public IEnumerable<Layer> GetLayers() => _layers.Values;
    
    public Layer? GetLayer(string name) => _layers.GetValueOrDefault(name);
}

/// <summary>
/// Represents a unit of measurement, such as m²/m² or kgC/m²
/// </summary>
public record Unit(string Name, string Symbol);

/// <summary>
/// Represents a data column in an output file
/// </summary>
public class Layer
{
    public string Name { get; }
    public Unit Unit { get; }
    public IReadOnlyList<DataPoint> DataPoints { get; }

    public Layer(string name, Unit unit, IReadOnlyList<DataPoint> dataPoints)
    {
        Name = name;
        Unit = unit;
        DataPoints = dataPoints;
    }
}

/// <summary>
/// Represents a single data point in time
/// </summary>
public record DataPoint(int Year, double Value);

/// <summary>
/// Defines the structure and metadata of an output file
/// </summary>
public record OutputFileMetadata
{
    /// <summary>
    /// The quantity being measured or simulated
    /// </summary>
    public required Quantity Quantity { get; init; }
}
