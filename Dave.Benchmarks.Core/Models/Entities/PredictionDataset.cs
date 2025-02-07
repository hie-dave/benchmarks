using System;
using Dave.Benchmarks.Core.Utilities;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a dataset containing model predictions.
/// </summary>
public class PredictionDataset : Dataset
{
    // Optional additional metadata
    public string? InputDataSource { get; set; }                    // Other input data sources

    /// <summary>
    /// Complete instruction file used to run the model.
    /// </summary>
    public byte[] Parameters { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Code patches applied to the repository when the model was run.
    /// </summary>
    public byte[] Patches { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The climate dataset used to run the model.
    /// </summary>
    public string ClimateDataset { get; set; } = string.Empty;

    /// <summary>
    /// The version of the model that was run.
    /// </summary>
    public string ModelVersion { get; set; } = string.Empty;

    // Helper methods for parameters
    public void SetParameters(string parameters)
    {
        Parameters = CompressionUtility.CompressText(parameters);
    }

    public string GetParameters()
    {
        return CompressionUtility.DecompressToText(Parameters);
    }

    // Helper methods for code patches
    public void SetCodePatches(string patches)
    {
        Patches = CompressionUtility.CompressText(patches);
    }

    public string GetCodePatches()
    {
        return CompressionUtility.DecompressToText(Patches);
    }
}
