using System;

namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Request model for creating a new model prediction dataset.
/// </summary>
public sealed class CreateDatasetRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string ModelVersion { get; init; }
    public required string ClimateDataset { get; init; }
    public required string SpatialResolution { get; init; }
    public required string TemporalResolution { get; init; }
    public required string Parameters { get; init; }
    public required byte[] CodePatches { get; init; }
}
