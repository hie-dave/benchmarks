namespace Dave.Benchmarks.Core.Models;

public class DataPoint
{
    public long Id { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    
    // Navigation properties
    public int DatasetId { get; set; }
    public Dataset Dataset { get; set; } = null!;
    public int VariableId { get; set; }
    public Variable Variable { get; set; } = null!;
}
