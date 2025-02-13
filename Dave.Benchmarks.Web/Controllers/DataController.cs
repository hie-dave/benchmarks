using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

public class DataController : Controller
{
    private readonly BenchmarksDbContext _dbContext;

    public DataController(BenchmarksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var datasets = await _dbContext.Datasets
            .Include(d => d.Variables)
            .AsSplitQuery()
            .ToListAsync();

        return View(datasets);
    }

    public async Task<IActionResult> Timeseries()
    {
        var datasets = await _dbContext.Datasets
            .Include(d => d.Variables)
                .ThenInclude(v => v.Layers)
            .AsSplitQuery()
            .ToListAsync();

        return View(datasets);
    }

    [HttpGet("api/data/datasets")]
    public async Task<ActionResult<IEnumerable<Dataset>>> GetDatasets()
    {
        var datasets = await _dbContext.Datasets
            .Include(d => d.Variables)
            .AsSplitQuery()
            .ToListAsync();

        // Ensure proper type information is preserved
        foreach (var dataset in datasets)
        {
            if (dataset is PredictionDataset prediction)
            {
                // Load prediction-specific navigation properties if needed
            }
            else if (dataset is ObservationDataset observation)
            {
                // Load observation-specific navigation properties if needed
            }
        }

        return Ok(datasets);
    }

    [HttpGet("api/data/datasets/{datasetId}/variables")]
    public async Task<ActionResult<IEnumerable<Variable>>> GetVariables(int datasetId)
    {
        var dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
                .ThenInclude(v => v.Layers)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        return Ok(dataset.Variables);
    }

    [HttpGet("api/data/datasets/{datasetId}/variables/{variableId}/data")]
    public async Task<ActionResult<IEnumerable<object>>> GetData(
        int datasetId,
        int variableId,
        [FromQuery] int? layerId = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        var variable = await _dbContext.Variables
            .Include(v => v.Layers)
            .FirstOrDefaultAsync(v => v.Id == variableId && v.DatasetId == datasetId);

        if (variable == null)
            return NotFound($"Variable {variableId} not found in dataset {datasetId}");

        var query = _dbContext.GridcellData
            .Where(d => d.VariableId == variableId)
            .Join(_dbContext.VariableLayers,
                d => d.LayerId,
                l => l.Id,
                (d, l) => new { d.Timestamp, d.Value, d.Latitude, d.Longitude, LayerName = l.Name, LayerId = l.Id });

        if (layerId.HasValue)
            query = query.Where(d => d.LayerId == layerId.Value);

        if (startTime.HasValue)
            query = query.Where(d => d.Timestamp >= startTime.Value);

        if (endTime.HasValue)
            query = query.Where(d => d.Timestamp <= endTime.Value);

        var data = await query
            .OrderBy(d => d.Timestamp)
            .Take(1000) // Limit to prevent overwhelming the browser
            .Select(d => new
            {
                timestamp = d.Timestamp.ToString("O"), // Use ISO 8601 format for reliable parsing
                variableName = variable.Name,
                layer = d.LayerName,
                value = d.Value,
                location = $"({d.Latitude:F2}, {d.Longitude:F2})"
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("api/data/datasets/{datasetId}/variables/{variableId}/timeresolution")]
    public async Task<ActionResult<object>> GetTimeResolution(int datasetId, int variableId)
    {
        var query = _dbContext.GridcellData
            .Where(d => d.VariableId == variableId)
            .OrderBy(d => d.Timestamp);

        // Get just the first two timestamps to determine the interval
        var timestamps = await query
            .Select(d => d.Timestamp)
            .Take(2)
            .ToListAsync();

        if (timestamps.Count < 2)
            return Ok(new { format = "g" }); // Default format if we don't have enough data

        var interval = timestamps[1] - timestamps[0];
        string format;

        if (interval.TotalDays >= 365)
            format = "yyyy"; // Annual
        else if (interval.TotalDays >= 28)
            format = "yyyy-MM"; // Monthly
        else if (interval.TotalHours >= 24)
            format = "yyyy-MM-dd"; // Daily
        else
            format = "g"; // Default format with time

        return Ok(new { format });
    }

    [HttpDelete("api/data/datasets/{datasetId}")]
    public async Task<IActionResult> DeleteDataset(int datasetId)
    {
        var dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
                .ThenInclude(v => v.Layers)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound();

        _dbContext.Datasets.Remove(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
