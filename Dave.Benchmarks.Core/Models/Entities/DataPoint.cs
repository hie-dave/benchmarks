using System;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Base class for all data points in the system.
/// </summary>
public abstract class DataPoint
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double Value { get; set; }

    // Navigation properties
    public int VariableId { get; set; }
    public Variable Variable { get; set; } = null!;
    public int LayerId { get; set; }
    public VariableLayer Layer { get; set; } = null!;
}
