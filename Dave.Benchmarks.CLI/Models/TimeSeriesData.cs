using System;
using System.Collections.Generic;

namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Represents a time series of data for a specific layer (e.g., a PFT)
/// </summary>
public class TimeSeries
{
    public string Name { get; }
    public string Units { get; }
    public IReadOnlyList<DataPoint> Points { get; }

    public TimeSeries(string name, string units, IReadOnlyList<DataPoint> points)
    {
        Name = name;
        Units = units;
        Points = points;
    }
}

/// <summary>
/// Represents a physical quantity (like LAI) containing multiple time series
/// </summary>
public class Quantity
{
    private readonly Dictionary<string, TimeSeries> _series = new();

    public string Name { get; }
    public string Description { get; }
    public string DefaultUnits { get; }

    public Quantity(string name, string description, string defaultUnits)
    {
        Name = name;
        Description = description;
        DefaultUnits = defaultUnits;
    }

    public void AddSeries(string name, IReadOnlyList<DataPoint> points)
    {
        _series[name] = new TimeSeries(name, DefaultUnits, points);
    }

    public IEnumerable<TimeSeries> GetSeries() => _series.Values;
    
    public TimeSeries? GetSeries(string name) => _series.GetValueOrDefault(name);
}
