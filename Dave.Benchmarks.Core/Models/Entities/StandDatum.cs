using System.ComponentModel.DataAnnotations.Schema;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a data point aggregated over all patches in a stand.
/// </summary>
[Table("StandData")]
public class StandDatum : DataPoint
{
    public int StandId { get; set; }
}
