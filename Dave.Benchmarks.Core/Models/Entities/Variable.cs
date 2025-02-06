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
    
    // Navigation properties
    public int DatasetId { get; set; }
    public Dataset Dataset { get; set; } = null!;
    public ICollection<Datum> Data { get; set; } = new List<Datum>();
}
