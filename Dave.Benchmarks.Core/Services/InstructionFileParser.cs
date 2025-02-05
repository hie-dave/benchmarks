using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dave.Benchmarks.Core.Services;

public class InstructionFileParser
{
    private readonly HashSet<string> _processedFiles = new();
    private static readonly Regex ImportRegex = new(@"^import\s+""([^""]+)""", RegexOptions.Compiled);

    public async Task<string> ParseInstructionFileAsync(string filePath)
    {
        _processedFiles.Clear();
        return await ParseFileAsync(filePath);
    }

    private async Task<string> ParseFileAsync(string filePath)
    {
        var absolutePath = Path.GetFullPath(filePath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Instruction file not found: {absolutePath}");
        }

        if (_processedFiles.Contains(absolutePath))
        {
            // Skip files we've already processed to avoid circular imports
            return string.Empty;
        }

        _processedFiles.Add(absolutePath);
        var baseDir = Path.GetDirectoryName(absolutePath)!;
        var sb = new StringBuilder();

        var lines = await File.ReadAllLinesAsync(absolutePath);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("!"))
            {
                sb.AppendLine(line);
                continue;
            }

            var match = ImportRegex.Match(trimmedLine);
            if (match.Success)
            {
                var importPath = match.Groups[1].Value;
                var fullPath = Path.GetFullPath(Path.Combine(baseDir, importPath));
                
                // Add a comment to show where this import came from
                sb.AppendLine($"! Imported from: {importPath}");
                
                // Recursively process the imported file
                var importedContent = await ParseFileAsync(fullPath);
                sb.AppendLine(importedContent);
                
                // Add a comment to show where the import ends
                sb.AppendLine($"! End of import: {importPath}");
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }
}
