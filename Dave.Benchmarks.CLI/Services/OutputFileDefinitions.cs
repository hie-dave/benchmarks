using System.Collections.Immutable;
using Dave.Benchmarks.CLI.Models;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// Provides metadata about known output file types
/// </summary>
public static class OutputFileDefinitions
{
    private static readonly ImmutableDictionary<string, OutputFileMetadata> Definitions;

    static OutputFileDefinitions()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, OutputFileMetadata>();

        AddPftOutput(builder, "lai", "Leaf Area Index", "Annual Leaf Area Index", "m2/m2");
        AddPftOutput(builder, "cmass", "Carbon Mass", "Total carbon mass in vegetation", "kgC/m2");
        AddPftOutput(builder, "dave_lai", "Leaf Area Index", "Daily Leaf Area Index", "m2/m2");

        Definitions = builder.ToImmutable();
    }

    /// <summary>
    /// Get metadata for a specific output file type
    /// </summary>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out)</param>
    /// <returns>Metadata about the output file structure, or null if not a known type</returns>
    /// <exception cref="InvalidOperationException">Thrown if no metadata is found for the specified type.</exception>
    public static OutputFileMetadata GetMetadata(string fileType)
    {
        if (Definitions.TryGetValue(fileType, out var quantity))
            return quantity;
        throw new InvalidOperationException($"Unable to find metadata for unknown output file type: {fileType}");
    }

    /// <summary>
    /// Register metadata for a PFT-level output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out)</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index")</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index")</param>
    /// <param name="units">The units of the output file (e.g., "m2/m2")</param>
    private static void AddPftOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new PftLayers(new Unit(units))));
    }

    /// <summary>
    /// Register metadata for a generic output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out)</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index")</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index")</param>
    /// <param name="units">The units of the output file (e.g., "m2/m2")</param>
    /// <param name="layers">The layer definitions for the output file.</param>
    private static void AddOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, ILayerDefinitions layers)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: layers));
    }
}
