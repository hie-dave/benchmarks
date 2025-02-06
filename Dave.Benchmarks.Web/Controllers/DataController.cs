using Dave.Benchmarks.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

public class DataController : Controller
{
    private readonly BenchmarksDbContext _context;

    public DataController(BenchmarksDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var datasets = await _context.Datasets
            .Include(d => d.Variables)
            .ToListAsync();
        return View(datasets);
    }

    [HttpGet]
    public async Task<IActionResult> GetDatasetData(int datasetId, int? variableId = null)
    {
        var query = _context.MeasurementPoints
            .Where(d => d.DatasetId == datasetId);

        if (variableId.HasValue)
        {
            query = query.Where(d => d.VariableId == variableId.Value);
        }

        var data = await query
            .Take(1000) // Limit initial load
            .OrderBy(d => d.Timestamp)
            .Select(d => new {
                d.Id,
                d.Timestamp,
                d.Latitude,
                d.Longitude,
                d.Value,
                VariableName = d.Variable.Name
            })
            .ToListAsync();

        return Json(data);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDataset(int id)
    {
        var dataset = await _context.Datasets.FindAsync(id);
        if (dataset == null)
        {
            return NotFound();
        }

        try
        {
            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Failed to delete dataset: " + ex.Message });
        }
    }
}
