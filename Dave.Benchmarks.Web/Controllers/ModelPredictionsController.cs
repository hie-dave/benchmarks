using System;
using System.Threading.Tasks;
using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models;
using Dave.Benchmarks.Core.Models.Importer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.Web.Controllers;

/// <summary>
/// Controller for managing model predictions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly ILogger<PredictionsController> _logger;
    private readonly BenchmarksDbContext _dbContext;

    public PredictionsController(
        ILogger<PredictionsController> logger,
        BenchmarksDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Imports a new model prediction dataset.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportModelPredictionRequest request)
    {
        _logger.LogInformation("Importing model prediction: {Name}", request.Name);

        var dataset = new ModelPredictionDataset
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            ModelVersion = request.ModelVersion,
            ClimateDataset = request.ClimateDataset,
            SpatialResolution = request.SpatialResolution,
            TemporalResolution = request.TemporalResolution,
            CodePatches = request.CodePatches
        };

        dataset.SetParameters(request.Parameters);

        // Save dataset
        _dbContext.ModelPredictions.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(new { Id = dataset.Id });
    }
}
