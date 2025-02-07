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
            .Where(d => d.VariableId == variableId);

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
                timestamp = d.Timestamp.ToString("g"),
                variableName = variable.Name,
                layer = variable.Layers.First(l => l.Id == d.LayerId).Name,
                value = d.Value,
                location = $"({d.Latitude:F2}, {d.Longitude:F2})"
            })
            .ToListAsync();

        return Ok(data);
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
