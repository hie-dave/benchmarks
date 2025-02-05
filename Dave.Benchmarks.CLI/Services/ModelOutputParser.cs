using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dave.Benchmarks.CLI.Models;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

public class ModelOutputParser
{
    private const string LonColumn = "Lon";
    private const string LatColumn = "Lat";
    private const string YearColumn = "Year";
    private const string DayColumn = "Day";

    private readonly ILogger<ModelOutputParser> _logger;

    public ModelOutputParser(ILogger<ModelOutputParser> logger)
    {
        _logger = logger;
    }

    public async Task<Quantity> ParseOutputFileAsync(string filePath)
    {
        var fileType = Path.GetFileNameWithoutExtension(filePath);
        var metadata = OutputFileDefinitions.GetMetadata(fileType);
        
        if (metadata == null)
        {
            throw new InvalidOperationException($"Unknown output file type: {fileType}");
        }

        var quantity = new TimeSeriesQuantity(
            metadata.Name,
            metadata.Description,
            metadata.DefaultUnits);

        var lines = await File.ReadAllLinesAsync(filePath);

        if (lines.Length < 2)
        {
            throw new InvalidDataException("File must contain at least a header row and one data row");
        }

        // Parse header row to get column indices
        var headers = lines[0].Split('\t', StringSplitOptions.RemoveEmptyEntries);
        var columnIndices = GetColumnIndices(headers);

        // Create a dictionary to hold data points for each series
        var seriesData = new Dictionary<string, List<DataPoint>>();
        foreach (var name in headers.Skip(columnIndices.RequiredColumns.Count)) // Skip required columns
        {
            seriesData[name] = new List<DataPoint>();
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != headers.Length)
            {
                throw new InvalidDataException($"Row {i + 1} has {values.Length} columns but header has {headers.Length}");
            }

            var point = ParseRequiredColumns(values, columnIndices, i + 1);

            // Parse data values for each series
            for (int j = columnIndices.RequiredColumns.Count; j < values.Length; j++)
            {
                if (!double.TryParse(values[j], out var value))
                {
                    throw new InvalidDataException($"Invalid value in row {i + 1}, column {j + 1}: {values[j]}");
                }

                seriesData[headers[j]].Add(point with { Value = value });
            }
        }

        // Add series to quantity
        foreach (var (name, points) in seriesData)
        {
            quantity.AddSeries(name, points);
        }

        return quantity;
    }

    private record ColumnIndices(
        IReadOnlyDictionary<string, int> RequiredColumns,
        IReadOnlyList<string> DataColumns);

    private ColumnIndices GetColumnIndices(string[] headers)
    {
        var requiredColumns = new Dictionary<string, int>();

        // Find required column indices
        foreach (var header in new[] { LonColumn, LatColumn, YearColumn })
        {
            var index = Array.IndexOf(headers, header);
            if (index == -1)
            {
                throw new InvalidDataException($"Required column '{header}' not found in header row");
            }
            requiredColumns[header] = index;
        }

        // Day column is optional
        var dayIndex = Array.IndexOf(headers, DayColumn);
        if (dayIndex != -1)
        {
            requiredColumns[DayColumn] = dayIndex;
        }

        // Get data column names (everything that's not a required column)
        var dataColumns = headers
            .Where(h => !requiredColumns.ContainsKey(h))
            .ToList();

        if (!dataColumns.Any())
        {
            throw new InvalidDataException("No data columns found in header row");
        }

        return new ColumnIndices(requiredColumns, dataColumns);
    }

    private DataPoint ParseRequiredColumns(string[] values, ColumnIndices indices, int rowNum)
    {
        if (!double.TryParse(values[indices.RequiredColumns[LonColumn]], out var lon))
        {
            throw new InvalidDataException($"Invalid longitude in row {rowNum}: {values[indices.RequiredColumns[LonColumn]]}");
        }

        if (!double.TryParse(values[indices.RequiredColumns[LatColumn]], out var lat))
        {
            throw new InvalidDataException($"Invalid latitude in row {rowNum}: {values[indices.RequiredColumns[LatColumn]]}");
        }

        if (!int.TryParse(values[indices.RequiredColumns[YearColumn]], out var year))
        {
            throw new InvalidDataException($"Invalid year in row {rowNum}: {values[indices.RequiredColumns[YearColumn]]}");
        }

        var day = 365; // Default to last day of year
        if (indices.RequiredColumns.TryGetValue(DayColumn, out var dayIndex))
        {
            if (!int.TryParse(values[dayIndex], out day))
            {
                throw new InvalidDataException($"Invalid day in row {rowNum}: {values[dayIndex]}");
            }
        }

        var timestamp = new DateTime(year, 1, 1).AddDays(day - 1); // day is 1-based
        return new DataPoint(lon, lat, timestamp, 0); // Value will be set later
    }
}
