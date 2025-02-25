using System;
using System.Collections.Generic;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a logical grouping of related datasets, typically from the same model run or experiment.
/// </summary>
public class DatasetGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Indicates whether this group is complete and should not accept new datasets.
    /// </summary>
    public bool IsComplete { get; set; }
    
    /// <summary>
    /// The datasets that belong to this group.
    /// </summary>
    public ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();
    
    /// <summary>
    /// Additional metadata about this group stored as a JSON document.
    /// This can include things like model version, climate scenario, etc.
    /// </summary>
    public string Metadata { get; set; } = "{}";
}
