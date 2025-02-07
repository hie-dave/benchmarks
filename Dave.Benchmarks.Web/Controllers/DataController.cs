using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class DataController : ControllerBase
{
    private readonly BenchmarksDbContext _dbContext;

    public DataController(BenchmarksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("datasets")]
    public async Task<ActionResult<IEnumerable<Dataset>>> GetDatasets()
    {
        var datasets = await _dbContext.Datasets
            .Include(d => d.Variables)
            .ToListAsync();

        return Ok(datasets);
    }

    [HttpGet("datasets/{datasetId}/variables")]
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

    [HttpGet("datasets/{datasetId}/variables/{variableId}/data")]
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

        // Build query based on variable level
        switch (variable.Level)
        {
            case AggregationLevel.Gridcell:
                var gridcellQuery = _dbContext.GridcellData
                    .Where(d => d.VariableId == variableId);

                if (layerId.HasValue)
                    gridcellQuery = gridcellQuery.Where(d => d.LayerId == layerId);
                if (startTime.HasValue)
                    gridcellQuery = gridcellQuery.Where(d => d.Timestamp >= startTime);
                if (endTime.HasValue)
                    gridcellQuery = gridcellQuery.Where(d => d.Timestamp <= endTime);

                return Ok(await gridcellQuery.ToListAsync());

            case AggregationLevel.Stand:
                var standQuery = _dbContext.StandData
                    .Where(d => d.VariableId == variableId);

                if (layerId.HasValue)
                    standQuery = standQuery.Where(d => d.LayerId == layerId);
                if (startTime.HasValue)
                    standQuery = standQuery.Where(d => d.Timestamp >= startTime);
                if (endTime.HasValue)
                    standQuery = standQuery.Where(d => d.Timestamp <= endTime);

                return Ok(await standQuery.ToListAsync());

            case AggregationLevel.Patch:
                var patchQuery = _dbContext.PatchData
                    .Where(d => d.VariableId == variableId);

                if (layerId.HasValue)
                    patchQuery = patchQuery.Where(d => d.LayerId == layerId);
                if (startTime.HasValue)
                    patchQuery = patchQuery.Where(d => d.Timestamp >= startTime);
                if (endTime.HasValue)
                    patchQuery = patchQuery.Where(d => d.Timestamp <= endTime);

                return Ok(await patchQuery.ToListAsync());

            default:
                return BadRequest($"Unknown variable level: {variable.Level}");
        }
    }
}
