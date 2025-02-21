using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dave.Benchmarks.Core.Models;
using Dave.Benchmarks.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.Core.Services;

/// <summary>
/// Parses LPJ-GUESS instruction files, handling imports and parameter definitions.
/// </summary>
public class InstructionFileParser
{
    private readonly HashSet<string> processedFiles = new();
    private static readonly Regex ImportRegex = new(@"^import\s+""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex ParameterRegex = new(@"^(\S+)\s+""?([^""]+)""?", RegexOptions.Compiled);
    private readonly Dictionary<string, string> parameters = new();
    private string fileName = string.Empty;
    private readonly ILogger<InstructionFileParser> logger;

    public InstructionFileParser(ILogger<InstructionFileParser> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Parses an instruction file and returns its contents with all imports resolved.
    /// </summary>
    /// <param name="filePath">Path to the instruction file to parse.</param>
    /// <returns>The processed instruction file contents.</returns>
    public async Task<string> ParseInstructionFileAsync(string filePath)
    {
        processedFiles.Clear();
        parameters.Clear();
        fileName = filePath;
        return await ParseFileAsync(filePath);
    }

    /// <summary>
    /// Gets the value of a parameter from the instruction file.
    /// </summary>
    /// <param name="parameterName">Name of the parameter to retrieve.</param>
    /// <returns>The parameter value if found, null otherwise.</returns>
    public bool TryGetParameterValue(string parameterName, [MaybeNullWhen(false)] out string value)
    {
        return parameters.TryGetValue(parameterName, out value);
    }

    /// <summary>
    /// Gets all parameters that match a specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match against parameter names.</param>
    /// <returns>Dictionary of matching parameter names and their values.</returns>
    public Dictionary<string, string> GetParametersByPrefix(string prefix)
    {
        Dictionary<string, string> result = new();
        foreach (var kvp in parameters)
            if (kvp.Key.StartsWith(prefix))
                result.Add(kvp.Key, kvp.Value);
        return result;
    }

    private async Task<string> ParseFileAsync(string filePath)
    {
        string absolutePath = Path.GetFullPath(filePath);
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException($"Instruction file not found: {absolutePath}");

        if (processedFiles.Contains(absolutePath))
            return string.Empty;

        processedFiles.Add(absolutePath);
        string baseDir = Path.GetDirectoryName(absolutePath)!;
        StringBuilder sb = new();

        string[] lines = await File.ReadAllLinesAsync(absolutePath);
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("!"))
            {
                sb.AppendLine(line);
                continue;
            }

            Match importMatch = ImportRegex.Match(trimmedLine);
            if (importMatch.Success)
            {
                string importPath = importMatch.Groups[1].Value;
                string fullPath = Path.GetFullPath(Path.Combine(baseDir, importPath));
                
                sb.AppendLine($"! Imported from: {importPath}");
                string importedContent = await ParseFileAsync(fullPath);
                sb.AppendLine(importedContent);
                sb.AppendLine($"! End of import: {importPath}");
            }
            else
            {
                // Try to parse as a parameter
                Match paramMatch = ParameterRegex.Match(trimmedLine);
                if (paramMatch.Success)
                {
                    string paramName = paramMatch.Groups[1].Value;
                    string paramValue = paramMatch.Groups[2].Value;
                    parameters[paramName] = paramValue;
                }
                
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    public string GetGridlist()
    {
        if (string.IsNullOrEmpty(fileName))
            throw new InvalidOperationException("Instruction file has not yet been parsed");

        if (TryGetParameterValue(ModelConstants.ParamGridlist, out string? gridlist))
            return gridlist;

        if (TryGetParameterValue(ModelConstants.ParamGridlistCf, out gridlist))
            return gridlist;

        ExceptionHelper.Throw<InvalidDataException>(logger, $"Instruction file {fileName} does not contain a gridlist parameter");
        throw new Exception(); // Can never happen
        // TODO: replace with .net 10 return never
    }
}
