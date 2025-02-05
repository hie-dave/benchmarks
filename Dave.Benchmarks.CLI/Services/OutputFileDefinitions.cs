using System.Collections.Immutable;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// Provides metadata about known output file types
/// </summary>
public static class OutputFileDefinitions
{
    private static readonly ImmutableDictionary<string, TimeSeriesQuantity> Definitions;

    static OutputFileDefinitions()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, TimeSeriesQuantity>();

        // LAI output file (lai.out)
        builder.Add("lai", new TimeSeriesQuantity(
            "Leaf Area Index",
            "One-sided green leaf area per unit ground surface area",
            "m²/m²"));

        // Carbon mass output file (cmass.out)
        builder.Add("cmass", new TimeSeriesQuantity(
            "Carbon Mass",
            "Total carbon mass in vegetation",
            "kgC/m²"));

        Definitions = builder.ToImmutable();
    }

    /// <summary>
    /// Get metadata for a specific output file type
    /// </summary>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out)</param>
    /// <returns>Metadata about the output file structure, or null if not a known type</returns>
    public static TimeSeriesQuantity? GetMetadata(string fileType)
    {
        return Definitions.GetValueOrDefault(fileType);
    }
}
