namespace Dave.Benchmarks.CLI.Models;

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
    /// Create a new output file metadata instance.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="name">Title of the output file.</param>
    /// <param name="description">Description of the output file.</param>
    /// <param name="layers">Layer metadata.</param>
    public OutputFileMetadata(string fileName, string name, string description, ILayerDefinitions layers)
    {
        FileName = fileName;
        Name = name;
        Description = description;
        Layers = layers;
    }
}
