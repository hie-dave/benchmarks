using System.Collections.Immutable;
using Dave.Benchmarks.Core.Models;

namespace Dave.Benchmarks.Core.Services;

/// <summary>
/// Provides metadata about known output file types
/// </summary>
public static class OutputFileDefinitions
{
    private static readonly ImmutableDictionary<string, OutputFileMetadata> Definitions;

    // Common units
    public static readonly Unit SquareMeterPerSquareMeter = new("Square meter per square meter", "m²/m²");
    public static readonly Unit KilogramCarbonPerSquareMeter = new("Kilogram carbon per square meter", "kgC/m²");

    static OutputFileDefinitions()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, OutputFileMetadata>();

        // LAI output file (lai.out)
        builder.Add("lai", new OutputFileMetadata
        {
            Quantity = new Quantity(
                "Leaf Area Index",
                "One-sided green leaf area per unit ground surface area",
                SquareMeterPerSquareMeter)
        });

        // Carbon mass output file (cmass.out)
        builder.Add("cmass", new OutputFileMetadata
        {
            Quantity = new Quantity(
                "Carbon Mass",
                "Total carbon mass in vegetation",
                KilogramCarbonPerSquareMeter)
        });

        Definitions = builder.ToImmutable();
    }

    /// <summary>
    /// Get metadata for a specific output file type
    /// </summary>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out)</param>
    /// <returns>Metadata about the output file structure, or null if not a known type</returns>
    public static OutputFileMetadata? GetMetadata(string fileType)
    {
        return Definitions.GetValueOrDefault(fileType);
    }
}
