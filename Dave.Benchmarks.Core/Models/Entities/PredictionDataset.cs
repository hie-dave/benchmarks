using Dave.Benchmarks.Core.Utilities;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Base class for all model prediction datasets.
/// </summary>
public abstract class PredictionDataset : Dataset
{
    public string ModelVersion { get; set; } = string.Empty;
    public string ClimateDataset { get; set; } = string.Empty;
    public byte[] Patches { get; set; } = Array.Empty<byte>();

    public void SetPatches(string patches) => 
        Patches = CompressionUtility.CompressText(patches);
    
    public string GetPatches() => 
        CompressionUtility.DecompressToText(Patches);
}
