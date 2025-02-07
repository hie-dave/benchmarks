using System.ComponentModel.DataAnnotations.Schema;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a data point for an individual patch.
/// </summary>
[Table("PatchData")]
public class PatchDatum : DataPoint
{
    public int StandId { get; set; }
    public int PatchId { get; set; }
}
