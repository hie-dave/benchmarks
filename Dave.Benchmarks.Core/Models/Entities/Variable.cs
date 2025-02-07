using System.Collections.Generic;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a variable in a dataset, containing metadata about the measurements.
/// </summary>
public class Variable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public AggregationLevel Level { get; set; }
    
    // Navigation properties
    public int DatasetId { get; set; }
    public Dataset Dataset { get; set; } = null!;
    public ICollection<VariableLayer> Layers { get; set; } = new List<VariableLayer>();
}
