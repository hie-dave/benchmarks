using System;
using System.Collections.Generic;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a collection of model predictions from a single model run.
/// </summary>
public class Dataset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public string ClimateDataset { get; set; } = string.Empty;
    public string SpatialResolution { get; set; } = string.Empty;
    public string TemporalResolution { get; set; } = string.Empty;
    public byte[] CompressedParameters { get; set; } = Array.Empty<byte>();
    public byte[] CompressedCodePatches { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
}
