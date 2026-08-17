using System.ComponentModel.DataAnnotations;
using Dave.Benchmarks.Core.Models.Entities;

namespace Dave.Benchmarks.Web.Models;

public class CreateObservationGroupRequest
{
    [Required, StringLength(128)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Source { get; set; } = string.Empty;
    [Required, StringLength(128)] public string Version { get; set; } = string.Empty;
    [StringLength(500)] public string Description { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}";
    public DatasetGroupKind Kind { get; set; }
}
