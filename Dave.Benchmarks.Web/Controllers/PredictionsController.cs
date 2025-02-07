using System;
using System.Threading.Tasks;
using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    /// Creates a new model prediction dataset.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateDataset([FromBody] CreateDatasetRequest request)
    {
        _logger.LogInformation("Creating model prediction dataset: {Name}", request.Name);

        var dataset = new PredictionDataset
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

        _dbContext.Predictions.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(new { Id = dataset.Id });
    }

    /// <summary>
    /// Adds a quantity into an existing model prediction dataset.
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddQuantity([FromBody] ImportModelPredictionRequest request)
    {
        var dataset = await _dbContext.Predictions
            .Include(d => d.Variables)
            .Include(d => d.Data)
            .FirstOrDefaultAsync(d => d.Id == request.DatasetId);

        if (dataset == null)
            return NotFound($"Dataset with ID {request.DatasetId} not found");

        _logger.LogInformation(
            "Importing quantity {Quantity} into dataset: {Name}", 
            request.Quantity.Name,
            dataset.Name);

        // Create variable for the quantity
        var variable = new Variable
        {
            Name = request.Quantity.Name,
            Description = request.Quantity.Description,
            Dataset = dataset
        };

        // Create data points for each layer's data
        foreach (Layer layer in request.Quantity.Layers)
        {
            variable.Units = layer.Unit.ToString();
            
            foreach (var point in layer.Data)
            {
                var measurementPoint = new Datum
                {
                    Dataset = dataset,
                    Variable = variable,
                    Longitude = point.Longitude,
                    Latitude = point.Latitude,
                    Timestamp = point.Timestamp,
                    Value = point.Value
                };
                dataset.Data.Add(measurementPoint);
            }
        }

        dataset.Variables.Add(variable);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
}
