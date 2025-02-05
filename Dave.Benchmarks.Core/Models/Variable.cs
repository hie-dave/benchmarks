namespace Dave.Benchmarks.Core.Models;

public class Variable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    
    // Navigation properties
    public int DatasetId { get; set; }
    public Dataset Dataset { get; set; } = null!;
    public ICollection<DataPoint> DataPoints { get; set; } = new List<DataPoint>();
}
