using System.Collections.Immutable;
using Dave.Benchmarks.Core.Services;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// Service for resolving output file types based on instruction file parameters.
/// </summary>
public class OutputFileTypeResolver : IOutputFileTypeResolver
{
    private readonly ILogger<OutputFileTypeResolver> logger;
    private ImmutableDictionary<string, string> filenamesToTypes;

    /// <summary>
    /// Creates a new instance of the OutputFileTypeResolver.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="parser">Parser for instruction files.</param>
    public OutputFileTypeResolver(ILogger<OutputFileTypeResolver> logger, InstructionFileParser parser)
    {
        this.logger = logger;
        filenamesToTypes = ImmutableDictionary<string, string>.Empty;
    }

    /// <summary>
    /// Builds a lookup table mapping output filenames to their corresponding
    /// file types.
    /// </summary>
    public void BuildLookupTable(InstructionFileParser parser)
    {
        // Get all known output file types.
        logger.LogTrace("Building output file name lookup table");

        IEnumerable<string> knownTypes = OutputFileDefinitions.GetAllFileTypes();
        logger.LogTrace("Discovered {count} known output file types", knownTypes.Count());

        // Get the actual file names from the instruction file.
        var builder = ImmutableDictionary.CreateBuilder<string, string>();
        foreach (string fileType in knownTypes)
            if (parser.TryGetParameterValue(fileType, out string? filename) && !string.IsNullOrEmpty(filename))
                builder.Add(filename, fileType);

        // Create the lookup table.
        filenamesToTypes = builder.ToImmutable();
        logger.LogTrace("Discovered {count} enabled output file types", filenamesToTypes.Count);
    }

    /// <summary>
    /// Gets the file type for a given output filename.
    /// </summary>
    /// <param name="filename">The output filename to get the type for.</param>
    /// <returns>The file type if found.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the filename is not found.</exception>
    public string GetFileType(string filename)
    {
        if (filenamesToTypes.TryGetValue(filename, out string? fileType))
            return fileType;
        throw new KeyNotFoundException($"Unable to find output file type for filename: {filename}");
    }

    /// <summary>
    /// Gets all unique file types in the lookup table.
    /// </summary>
    /// <returns>A HashSet of all unique file types.</returns>
    public HashSet<string> GetAllFileTypes()
    {
        return new HashSet<string>(filenamesToTypes.Values);
    }
}
