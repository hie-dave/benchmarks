using System;
using System.Collections.Generic;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a collection of data points.
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
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();

    /// <summary>
    /// The group this dataset belongs to, if any.
    /// </summary>
    public DatasetGroup? Group { get; set; }
    public int? GroupId { get; set; }
}
