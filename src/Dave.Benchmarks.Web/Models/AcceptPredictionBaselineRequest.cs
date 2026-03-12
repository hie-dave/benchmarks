using System.ComponentModel.DataAnnotations;

namespace Dave.Benchmarks.Web.Models;

public class AcceptPredictionBaselineRequest
{
    [Required]
    public int DatasetId { get; set; }

    [Required]
    [StringLength(256)]
    public string AcceptedBy { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? AcceptedReason { get; set; }

    [StringLength(128)]
    public string? AcceptedFromPipelineId { get; set; }
}
