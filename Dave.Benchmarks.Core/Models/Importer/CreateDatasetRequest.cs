using System;

namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Request model for creating a new dataset.
/// </summary>
public sealed class CreateDatasetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string ClimateDataset { get; set; } = string.Empty;
    public string SpatialResolution { get; set; } = string.Empty;
    public string TemporalResolution { get; set; } = string.Empty;
    public byte[] CompressedParameters { get; set; } = Array.Empty<byte>();
    public byte[] CompressedCodePatches { get; set; } = Array.Empty<byte>();
}
