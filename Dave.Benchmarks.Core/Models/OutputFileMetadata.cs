using Dave.Benchmarks.Core.Models.Entities;

namespace Dave.Benchmarks.Core.Models;

/// <summary>
/// Metadata about an output file.
/// </summary>
public class OutputFileMetadata
{
    /// <summary>
    /// The file name.
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// Title of the output file.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Description of the output file.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Layer metadata.
    /// </summary>
    public ILayerDefinitions Layers { get; init; }

    /// <summary>
    /// The level at which data in this file is aggregated.
    /// </summary>
    public AggregationLevel Level { get; init; }

    /// <summary>
    /// The temporal resolution of the data.
    /// </summary>
    public TemporalResolution TemporalResolution { get; init; }

    /// <summary>
    /// Create a new output file metadata instance.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="name">Title of the output file.</param>
    /// <param name="description">Description of the output file.</param>
    /// <param name="layers">Layer metadata.</param>
    /// <param name="level">The level at which data is aggregated.</param>
    /// <param name="resolution">The temporal resolution of the data.</param>
    public OutputFileMetadata(
        string fileName,
        string name,
        string description,
        ILayerDefinitions layers,
        AggregationLevel level,
        TemporalResolution resolution)
    {
        FileName = fileName;
        Name = name;
        Description = description;
        Layers = layers;
        Level = level;
        TemporalResolution = resolution;
    }
}
