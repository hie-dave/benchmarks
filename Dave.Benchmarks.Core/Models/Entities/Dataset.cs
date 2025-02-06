using System;
using System.Collections.Generic;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Base class for all datasets in the system.
/// </summary>
public abstract class Dataset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string SpatialResolution { get; set; } = string.Empty;
    public string TemporalResolution { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<Datum> Data { get; set; } = new List<Datum>();
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
}
