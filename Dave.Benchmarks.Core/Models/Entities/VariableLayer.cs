namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a specific layer within a variable (e.g., a specific PFT for LAI).
/// </summary>
public class VariableLayer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public int VariableId { get; set; }
    public Variable Variable { get; set; } = null!;
}
