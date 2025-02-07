using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly BenchmarksDbContext _dbContext;
    private readonly ILogger<PredictionsController> logger;

    public PredictionsController(
        BenchmarksDbContext dbContext,
        ILogger<PredictionsController> logger)
    {
        _dbContext = dbContext;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Dataset>> Create([FromBody] CreateDatasetRequest request)
    {
        var dataset = new Dataset
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            ModelVersion = request.ModelVersion,
            ClimateDataset = request.ClimateDataset,
            SpatialResolution = request.SpatialResolution,
            TemporalResolution = request.TemporalResolution,
            CompressedParameters = request.CompressedParameters,
            CompressedCodePatches = request.CompressedCodePatches
        };

        _dbContext.Datasets.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(dataset);
    }

    [HttpPost("{datasetId}/quantities")]
    public async Task<ActionResult> AddQuantity(int datasetId, [FromBody] Quantity quantity)
    {
        var dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        // Check if variable exists
        Variable? variable = dataset.Variables
            .FirstOrDefault(v => v.Name == quantity.Name);

        if (!quantity.Layers.Any())
            throw new InvalidOperationException("At least one layer is required");

        if (quantity.Layers.GroupBy(l => l.Unit).Count() > 1)
            throw new InvalidOperationException("All layers must have the same units (TODO: we could support this in the future)");

        if (variable == null)
        {
            variable = new Variable
            {
                Name = quantity.Name,
                Description = quantity.Description,
                Units = quantity.Layers.First().Unit.Name,
                Level = quantity.Level,
                Dataset = dataset
            };

            _dbContext.Variables.Add(variable);
        }

        // Add layers if they don't exist
        foreach (Layer layerData in quantity.Layers)
        {
            VariableLayer? layer = variable.Layers
                .FirstOrDefault(l => l.Name == layerData.Name);

            if (layer == null)
            {
                layer = new VariableLayer
                {
                    Name = layerData.Name,
                    // TODO: implement layer-level descriptions
                    Description = layerData.Name,
                    Variable = variable
                };
                _dbContext.VariableLayers.Add(layer);
            }

            // Add data points based on variable level
            switch (quantity.Level)
            {
                case AggregationLevel.Gridcell:
                    foreach (var point in layerData.Data)
                    {
                        var datum = new GridcellDatum
                        {
                            Variable = variable,
                            Layer = layer,
                            Timestamp = point.Timestamp,
                            Longitude = point.Longitude,
                            Latitude = point.Latitude,
                            Value = point.Value
                        };
                        _dbContext.GridcellData.Add(datum);
                    }
                    break;

                case AggregationLevel.Stand:
                    foreach (var point in layerData.Data)
                    {
                        var datum = new StandDatum
                        {
                            Variable = variable,
                            Layer = layer,
                            Timestamp = point.Timestamp,
                            Longitude = point.Longitude,
                            Latitude = point.Latitude,
                            StandId = point.Stand ?? throw new ArgumentException("Stand ID is required"),
                            Value = point.Value
                        };
                        _dbContext.StandData.Add(datum);
                    }
                    break;

                case AggregationLevel.Patch:
                    foreach (var point in layerData.Data)
                    {
                        var datum = new PatchDatum
                        {
                            Variable = variable,
                            Layer = layer,
                            Timestamp = point.Timestamp,
                            Longitude = point.Longitude,
                            Latitude = point.Latitude,
                            StandId = point.Stand ?? throw new ArgumentException("Stand ID is required"),
                            PatchId = point.Patch ?? throw new ArgumentException("Patch ID is required"),
                            Value = point.Value
                        };
                        _dbContext.PatchData.Add(datum);
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown variable level: {variable.Level}");
            }
        }

        await _dbContext.SaveChangesAsync();
        return Ok();
    }
}
