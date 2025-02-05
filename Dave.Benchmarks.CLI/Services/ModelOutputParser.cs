using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dave.Benchmarks.CLI.Models;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

public class ModelOutputParser
{
    private const string lonColumn = "Lon";
    private const string latColumn = "Lat";
    private const string yearColumn = "Year";
    private const string dayColumn = "Day";

    private readonly ILogger<ModelOutputParser> logger;

    public ModelOutputParser(ILogger<ModelOutputParser> logger)
    {
        this.logger = logger;
    }

    public async Task<Quantity> ParseOutputFileAsync(string filePath)
    {
        logger.LogInformation("Parsing output file: {filePath}", filePath);
        string? fileType = Path.GetFileNameWithoutExtension(filePath);

        using var _ = logger.BeginScope("{fileType}", fileType);

        try
        {
            return await ParseOutputFileInternalAsync(filePath, fileType);
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            // Log and rethrow unexpected exceptions
            // InvalidDataException is already logged by ExceptionHelper
            logger.LogError(ex, "Failed to parse output file: {filePath} - {ex.Message}", filePath, ex.Message);
            throw;
        }
    }

    private async Task<Quantity> ParseOutputFileInternalAsync(string filePath, string fileType)
    {
        logger.LogDebug("Retrieving output file metadata");
        OutputFileMetadata metadata = OutputFileDefinitions.GetMetadata(fileType);

        logger.LogDebug("Reading output file");
        string[] lines = await File.ReadAllLinesAsync(filePath);
        if (lines.Length < 2)
            ExceptionHelper.Throw<InvalidDataException>(logger, "File must contain at least a header row and one data row");

        // Parse header row to get column indices.
        logger.LogDebug("Parsing header row");
        string[] headers = SplitLine(lines[0]);
        IReadOnlyDictionary<string, int> indices = GetColumnIndices(headers);
        string[] dataColumns = headers.Where(metadata.Layers.IsDataLayer).ToArray();

        // Create a dictionary to hold data points for each series
        Dictionary<string, List<DataPoint>> seriesData = [];

        // Skip required columns
        foreach (string name in dataColumns)
            seriesData[name] = new List<DataPoint>();

        // Parse data rows.
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = SplitLine(lines[i]);
            using var __ = logger.BeginScope("Row {i}", i);

            if (values.Length != headers.Length)
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid number of columns: has {values.Length} columns but header has {headers.Length}");

            // Parse required columns.
            Coordinate point = ParseRequiredColumns(values, indices);

            // Parse data values for each series.
            logger.LogTrace("Parsing data values");
            foreach (string name in dataColumns)
            {
                if (!double.TryParse(values[indices[name]], out double value))
                    ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid value: failed to parse double: {values[indices[name]]}");

                seriesData[name].Add(new DataPoint(point.Timestamp, point.Lon, point.Lat, value));
            }
        }

        logger.LogDebug("File parsed successfully");
    
        // Add series to quantity.
        List<Layer> layers = [];
        foreach ((string name, List<DataPoint> points) in seriesData)
            layers.Add(new Layer(name, metadata.Layers.GetUnits(name), points));

        return new Quantity(metadata.Name, metadata.Description, layers);
    }

    /// <summary>
    /// Split a line of text into individual columns, without parsing any values.
    /// </summary>
    /// <param name="line">The line of text to split.</param>
    /// <returns>An array of columns.</returns>
    private string[] SplitLine(string line)
    {
        return line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Get column indices from a header row.
    /// </summary>
    /// <param name="headers">The headers of the file.</param>
    /// <returns>A dictionary mapping column names to column indices.</returns>
    private IReadOnlyDictionary<string, int> GetColumnIndices(string[] headers)
    {
        Dictionary<string, int> indices = [];

        // Find required column indices.
        for (int i = 0; i < headers.Length; i++)
            indices[headers[i]] = i;

        return indices;
    }

    /// <summary>
    /// The coordinate defining the spatio-temporal location of a data point.
    /// </summary>
    /// <param name="Lon">Longitude in degrees.</param>
    /// <param name="Lat">Latitude in degrees.</param>
    /// <param name="Timestamp">Date/Time of the data point.</param>
    private record Coordinate(double Lon, double Lat, DateTime Timestamp);

    /// <summary>
    /// Parse required columns from a row of an output file.
    /// </summary>
    /// <param name="values">The raw values from the row.</param>
    /// <param name="indices">The column indices for the required columns.</param>
    /// <returns>A coordinate representing the location of the data point.</returns>
    /// <exception cref="InvalidDataException">Thrown if the required columns cannot be parsed or contain invalid values.</exception>
    private Coordinate ParseRequiredColumns(string[] values, IReadOnlyDictionary<string, int> indices)
    {
        logger.LogTrace("Parsing coordinates");
        if (!double.TryParse(values[indices[lonColumn]], out var lon))
            ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid longitude: {values[indices[lonColumn]]}");

        if (!double.TryParse(values[indices[latColumn]], out var lat))
            ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid latitude: {values[indices[latColumn]]}");

        if (!int.TryParse(values[indices[yearColumn]], out var year))
            ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid year: {values[indices[yearColumn]]}");

        // Default to last day of year.
        int day = 364;
        if (indices.TryGetValue(dayColumn, out var dayIndex))
            if (!int.TryParse(values[dayIndex], out day))
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid day: {values[dayIndex]}");

        if (lon < 0 || lon > 360)
            ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid longitude: {values[indices[lonColumn]]}");

        if (lat < -90 || lat > 90)
            ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid latitude: {values[indices[latColumn]]}");

        if (day < 0 || day > 365)
            ExceptionHelper.Throw<InvalidDataException>(logger, $"Invalid day: {values[dayIndex]}");

        DateTime timestamp = new DateTime(year, 1, 1).AddDays(day);
        return new Coordinate(lon, lat, timestamp);
    }
}
