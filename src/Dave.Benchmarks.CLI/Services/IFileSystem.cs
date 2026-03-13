namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// Abstraction around filesystem access for ImportHandler.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Enumerates files in the specified directory matching the search pattern and search option.
    /// </summary>
    /// <param name="path">The directory path to search.</param>
    /// <param name="searchPattern">The search pattern to match files.</param>
    /// <param name="searchOption">Specifies whether to search all subdirectories or only the top directory.</param>
    /// <returns>An enumerable collection of file paths that match the search criteria.</returns>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Gets the last write time of the specified file or directory.
    /// </summary>
    /// <param name="path">The path to the file or directory.</param>
    /// <returns>The last write time of the file or directory.</returns>
    DateTime GetLastWriteTime(string path);
}
