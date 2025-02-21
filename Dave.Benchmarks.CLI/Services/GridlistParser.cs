using System.Globalization;
using Dave.Benchmarks.CLI.Models;
using Dave.Benchmarks.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

public class GridlistParser
{
    private readonly ILogger<GridlistParser> logger;

    public GridlistParser(ILogger<GridlistParser> logger)
    {
        this.logger = logger;
    }

    public async Task<IEnumerable<Coordinate>> Parse(string gridlist)
    {
        logger.LogInformation("Parsing gridlist file {gridlist}", gridlist);
        using var _ = logger.BeginScope(Path.GetFileName(gridlist));

        string[] lines = await File.ReadAllLinesAsync(gridlist);
        Coordinate[] coordinates = new Coordinate[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // Skip empty lines.
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(' ', '\t');
            if (parts.Length < 2)
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Parser error: line {i + 1}: lines must contain at least 2 parts (has {parts.Length})");
            if (!double.TryParse(parts[0], CultureInfo.InvariantCulture, out double lon))
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Parser error: line {i + 1}: failed to parse longitude from '{parts[0]}'");
            if (!double.TryParse(parts[1], CultureInfo.InvariantCulture, out double lat))
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Parser error: line {i + 1}: failed to parse latitude from '{parts[1]}'");
            if (lon < -180 || lon > 180)
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Parser error: line {i + 1}: longitude must be in range [-180, 180], but was: {lon}");
            if (lat < -90 || lat > 90)
                ExceptionHelper.Throw<InvalidDataException>(logger, $"Parser error: line {i + 1}: latitude must be in range [-90, 90], but was: {lat}");
            coordinates[i] = new Coordinate() { Longitude = lon, Latitude = lat };
        }
        return coordinates;
    }
}
