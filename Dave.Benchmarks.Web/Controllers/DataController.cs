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

        // Load type-specific navigation properties
        foreach (var dataset in datasets)
        {
            switch (dataset)
            {
                case SiteRunDataset siteDataset:
                    await _dbContext.Entry(siteDataset)
                        .Collection(d => d.Sites)
                        .LoadAsync();
                    break;

                case GriddedDataset griddedDataset:
                    await _dbContext.Entry(griddedDataset)
                        .Collection(d => d.ClimateScenarios)
                        .LoadAsync();
                    break;
            }
        }

        return Ok(datasets);
    }

    [HttpGet("api/data/sites")]
    public async Task<ActionResult<IEnumerable<SiteRun>>> GetSites(int datasetId)
    {
        var dataset = await _dbContext.Datasets
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset is not SiteRunDataset siteDataset)
            return BadRequest("Dataset is not a site run dataset");

        var sites = await _dbContext.SiteRuns
            .Where(s => s.DatasetId == datasetId)
            .ToListAsync();

        return Ok(sites);
    }

    [HttpGet("api/data/scenarios")]
    public async Task<ActionResult<IEnumerable<ClimateScenario>>> GetScenarios(
        int datasetId)
    {
        var dataset = await _dbContext.Datasets
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset is not GriddedDataset griddedDataset)
            return BadRequest("Dataset is not a gridded dataset");

        var scenarios = await _dbContext.ClimateScenarios
            .Where(s => s.DatasetId == datasetId)
            .ToListAsync();

        return Ok(scenarios);
    }

    [HttpGet("api/data/variables")]
    public async Task<ActionResult<IEnumerable<Variable>>> GetVariables(int datasetId)
    {
        var variables = await _dbContext.Variables
            .Include(v => v.Layers)
            .Where(v => v.DatasetId == datasetId)
            .ToListAsync();

        return Ok(variables);
    }

    [HttpGet("api/data/data")]
    public async Task<ActionResult<IEnumerable<Datum>>> GetData(
        int datasetId,
        int variableId,
        int? layerId = null)
    {
        var query = _dbContext.GridcellData
            .Where(d => d.Variable.DatasetId == datasetId && 
                       d.VariableId == variableId);

        if (layerId.HasValue)
            query = query.Where(d => d.LayerId == layerId.Value);

        var data = await query.ToListAsync();
        return Ok(data);
    }

    [HttpDelete("api/data/datasets/{id}")]
    public async Task<ActionResult> DeleteDataset(int id)
    {
        var dataset = await _dbContext.Datasets.FindAsync(id);
        if (dataset == null)
            return NotFound();

        _dbContext.Datasets.Remove(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
}
