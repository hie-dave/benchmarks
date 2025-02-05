using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dave.Benchmarks.Core.Models;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.Core.Services;

public class ModelOutputParser
{
    private readonly ILogger<ModelOutputParser> _logger;

    public ModelOutputParser(ILogger<ModelOutputParser> logger)
    {
        _logger = logger;
    }

    public async Task<Quantity> ParseOutputFileAsync(string filePath, int datasetId)
    {
        var fileType = Path.GetFileNameWithoutExtension(filePath);
        var metadata = OutputFileDefinitions.GetMetadata(fileType);
        
        if (metadata == null)
        {
            throw new InvalidOperationException($"Unknown output file type: {fileType}");
        }

        var quantity = metadata.Quantity;
        var lines = await File.ReadAllLinesAsync(filePath);

        if (lines.Length < 2)
        {
            throw new InvalidDataException("File must contain at least a header row and one data row");
        }

        // Parse header row to get layer names
        var layerNames = lines[0].Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (layerNames.Length < 2) // Year column + at least one data column
        {
            throw new InvalidDataException("Header row must contain Year and at least one data column");
        }

        // Create a dictionary to hold data points for each layer
        var layerData = new Dictionary<string, List<DataPoint>>();
        foreach (var name in layerNames.Skip(1)) // Skip Year column
        {
            layerData[name] = new List<DataPoint>();
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != layerNames.Length)
            {
                throw new InvalidDataException($"Row {i + 1} has {values.Length} columns but header has {layerNames.Length}");
            }

            if (!int.TryParse(values[0], out var year))
            {
                throw new InvalidDataException($"Invalid year value in row {i + 1}: {values[0]}");
            }

            var timestamp = new DateTime(year, 1, 1); // Use start of year as timestamp

            for (int j = 1; j < values.Length; j++)
            {
                if (!double.TryParse(values[j], out var value))
                {
                    throw new InvalidDataException($"Invalid value in row {i + 1}, column {j + 1}: {values[j]}");
                }

                layerData[layerNames[j]].Add(new DataPoint
                {
                    DatasetId = datasetId,
                    Timestamp = timestamp,
                    Value = value,
                    // Note: VariableId will be set later when variables are created
                });
            }
        }

        // Add layers to quantity
        foreach (var (name, dataPoints) in layerData)
        {
            quantity.AddLayer(name, dataPoints);
        }

        return quantity;
    }
}
