using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.Web.Controllers;

public class DataController : Controller
{
    private readonly BenchmarksDbContext _dbContext;
    private readonly ILogger<DataController> _logger;

    public DataController(BenchmarksDbContext dbContext, ILogger<DataController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var datasets = await _dbContext.Datasets
            .Include(d => d.Variables)
            .Include(d => d.Group)
            .AsSplitQuery()
            .ToListAsync();

        // Group datasets
        var groupedDatasets = datasets
            .GroupBy(d => d.Group)
            .OrderBy(g => g.Key?.Name ?? "")
            .ToList();

        return View(groupedDatasets);
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
            .Include(d => d.Group)
            .AsSplitQuery()
            .ToListAsync();

        return Ok(datasets);
    }

    [HttpGet("api/data/groups")]
    public async Task<ActionResult<IEnumerable<DatasetGroup>>> GetGroups()
    {
        var groups = await _dbContext.DatasetGroups
            .Include(g => g.Datasets)
            .AsSplitQuery()
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("api/data/group/{groupId}")]
    public async Task<ActionResult<DatasetGroup>> GetGroup(int groupId)
    {
        var group = await _dbContext.DatasetGroups
            .Include(g => g.Datasets)
                .ThenInclude(d => d.Variables)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return NotFound($"Group {groupId} not found");

        return Ok(group);
    }

    [HttpGet("api/data/dataset/{datasetId}/metadata")]
    public async Task<ActionResult<object>> GetDatasetMetadata(int datasetId)
    {
        var dataset = await _dbContext.Datasets
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        if (dataset is not PredictionDataset predictionDataset)
            return BadRequest("Dataset is not a prediction dataset");

        // Parse the metadata JSON
        var metadata = JsonSerializer.Deserialize<object>(predictionDataset.Metadata);
        return Ok(metadata);
    }

    [HttpGet("api/data/dataset/{datasetId}/variables")]
    public async Task<ActionResult<IEnumerable<Variable>>> GetVariables(int datasetId)
    {
        // Load variables directly from DbContext to get proper IQueryable
        var variables = await _dbContext.Variables
            .Include(v => v.Layers)
            .Where(v => v.DatasetId == datasetId)
            .OrderBy(v => v.Name)
            .ToListAsync();

        if (!variables.Any())
            return NotFound($"No variables found for dataset {datasetId}");

        return Ok(variables);
    }

    [HttpGet("api/data/layers")]
    public async Task<ActionResult<IEnumerable<VariableLayer>>> GetLayers(
        int variableId)
    {
        var variable = await _dbContext.Variables
            .Include(v => v.Layers)
            .FirstOrDefaultAsync(v => v.Id == variableId);

        if (variable == null)
            return NotFound($"Variable {variableId} not found");

        return Ok(variable.Layers);
    }

    [HttpGet("api/data/data")]
    public async Task<ActionResult<IEnumerable<Datum>>> GetData(
        int datasetId,
        int variableId,
        int? layerId = null,
        DateTime? start = null,
        DateTime? end = null)
    {
        var query = _dbContext.Set<Datum>()
            .Where(d => d.VariableId == variableId);

        if (layerId.HasValue)
            query = query.Where(d => d.LayerId == layerId.Value);

        if (start.HasValue)
            query = query.Where(d => d.Timestamp >= start.Value);

        if (end.HasValue)
            query = query.Where(d => d.Timestamp <= end.Value);

        var data = await query.ToListAsync();
        return Ok(data);
    }

    [HttpGet("api/data/dataset/{datasetId}")]
    public async Task<ActionResult<Dataset>> GetDataset(int datasetId)
    {
        var dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
            .Include(d => d.Group)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        return Ok(dataset);
    }

    [HttpGet("api/data/dataset/{datasetId}/variable/{variableId}/data")]
    public async Task<ActionResult<IEnumerable<object>>> GetVariableData(int datasetId, int variableId)
    {
        var variable = await _dbContext.Variables
            .Include(v => v.Dataset)
            .Include(v => v.Layers)
            .FirstOrDefaultAsync(v => v.Id == variableId && v.DatasetId == datasetId);

        if (variable == null)
            return NotFound($"Variable {variableId} not found in dataset {datasetId}");

        // Get the first 1000 data points for this variable
        IQueryable<object> query;
        
        switch (variable.Level)
        {
            case AggregationLevel.Gridcell:
                query = _dbContext.GridcellData
                    .Where(d => d.VariableId == variableId)
                    .Include(d => d.Layer)
                    .OrderBy(d => d.Timestamp)
                    .Take(1000)
                    .Select(d => new
                    {
                        d.Timestamp,
                        d.Value,
                        d.Latitude,
                        d.Longitude,
                        Layer = d.Layer.Name
                    });
                break;

            case AggregationLevel.Stand:
                query = _dbContext.StandData
                    .Where(d => d.VariableId == variableId)
                    .Include(d => d.Layer)
                    .OrderBy(d => d.Timestamp)
                    .Take(1000)
                    .Select(d => new
                    {
                        d.Timestamp,
                        d.Value,
                        d.Latitude,
                        d.Longitude,
                        d.StandId,
                        Layer = d.Layer.Name
                    });
                break;

            case AggregationLevel.Patch:
                query = _dbContext.PatchData
                    .Where(d => d.VariableId == variableId)
                    .Include(d => d.Layer)
                    .OrderBy(d => d.Timestamp)
                    .Take(1000)
                    .Select(d => new
                    {
                        d.Timestamp,
                        d.Value,
                        d.Latitude,
                        d.Longitude,
                        d.StandId,
                        d.PatchId,
                        Layer = d.Layer.Name
                    });
                break;

            case AggregationLevel.Individual:
                query = _dbContext.IndividualData
                    .Where(d => d.VariableId == variableId)
                    .Include(d => d.Layer)
                    .Include(d => d.Individual)
                        .ThenInclude(i => i.Pft)
                    .OrderBy(d => d.Timestamp)
                    .Take(1000)
                    .Select(d => new
                    {
                        d.Timestamp,
                        d.Value,
                        d.Latitude,
                        d.Longitude,
                        d.StandId,
                        d.PatchId,
                        d.IndividualId,
                        Pft = d.Individual.Pft.Name,
                        Layer = d.Layer.Name
                    });
                break;

            default:
                return BadRequest($"Unknown aggregation level: {variable.Level}");
        }

        // Execute query and get data
        var data = await query.ToListAsync();

        // Log the query results
        _logger.LogInformation(
            "Retrieved {Count} data points for variable {VariableId} (Level: {Level})",
            data.Count,
            variableId,
            variable.Level);

        if (!data.Any())
        {
            _logger.LogInformation(
                "No data found for variable {VariableId} in dataset {DatasetId}",
                variableId,
                datasetId);

            return Ok(Array.Empty<object>());
        }

        return Ok(data);
    }

    private async Task<int> GetVariableRowCount(int variableId)
    {
        var variable = await _dbContext.Variables
            .FirstOrDefaultAsync(v => v.Id == variableId);

        if (variable == null)
            return 0;

        return variable.Level switch
        {
            AggregationLevel.Gridcell => await _dbContext.GridcellData
                .Where(d => d.VariableId == variableId)
                .CountAsync(),
            AggregationLevel.Stand => await _dbContext.StandData
                .Where(d => d.VariableId == variableId)
                .CountAsync(),
            AggregationLevel.Patch => await _dbContext.PatchData
                .Where(d => d.VariableId == variableId)
                .CountAsync(),
            AggregationLevel.Individual => await _dbContext.IndividualData
                .Where(d => d.VariableId == variableId)
                .CountAsync(),
            _ => 0
        };
    }

    [HttpDelete("api/data/dataset/{id}")]
    public async Task<ActionResult> DeleteDataset(int id)
    {
        var dataset = await _dbContext.Datasets.FindAsync(id);
        if (dataset == null)
            return NotFound();

        try 
        {
            _dbContext.Datasets.Remove(dataset);
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("api/data/group/{id}")]
    public async Task<ActionResult> DeleteGroup(int id)
    {
        var group = await _dbContext.DatasetGroups
            .Include(g => g.Datasets)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return NotFound();

        try 
        {
            // Note: DB context is not configured for cascade deletion of
            // related datasets by default, so we need to manually delete them.
            _dbContext.Datasets.RemoveRange(group.Datasets);
            _dbContext.DatasetGroups.Remove(group);
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }
}
