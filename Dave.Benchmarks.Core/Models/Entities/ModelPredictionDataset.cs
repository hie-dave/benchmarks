using System;
using Dave.Benchmarks.Core.Utilities;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a dataset containing model predictions.
/// </summary>
public class ModelPredictionDataset : Dataset
{
    // Required for reproducibility
    public string ModelVersion { get; set; } = string.Empty;        // Git hash or version number
    public byte[] CompressedParameters { get; set; } = Array.Empty<byte>();  // Compressed parameter set
    public string ClimateDataset { get; set; } = string.Empty;      // Name/version of climate dataset used
    public byte[] CodePatches { get; set; } = Array.Empty<byte>();  // Compressed git patches/diffs

    // Optional additional metadata
    public string? InputDataSource { get; set; }                    // Other input data sources

    // Helper methods for parameters
    public void SetParameters(string parameters)
    {
        CompressedParameters = CompressionUtility.CompressText(parameters);
    }

    public string GetParameters()
    {
        return CompressionUtility.DecompressToText(CompressedParameters);
    }

    // Helper methods for code patches
    public void SetCodePatches(string patches)
    {
        CodePatches = CompressionUtility.CompressText(patches);
    }

    public string GetCodePatches()
    {
        return CompressionUtility.DecompressToText(CodePatches);
    }
}
