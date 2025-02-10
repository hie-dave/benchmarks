using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("create")]
    public async Task<ActionResult<PredictionDataset>> Create([FromBody] CreateDatasetRequest request)
    {
        var dataset = new PredictionDataset
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            ModelVersion = request.ModelVersion,
            ClimateDataset = request.ClimateDataset,
            SpatialResolution = request.SpatialResolution,
            TemporalResolution = request.TemporalResolution,
            Parameters = request.CompressedParameters,
            Patches = request.CompressedCodePatches
        };

        _dbContext.Datasets.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(dataset);
    }

    [HttpPost("{datasetId}/add")]
    public async Task<ActionResult> AddQuantity(int datasetId, [FromBody] Quantity quantity)
    {
        Dataset? dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        // Handle individual-level data validation and creation
        if (quantity.Level == AggregationLevel.Individual)
        {
            if (quantity.IndividualPfts == null)
                return BadRequest("Individual-level data must include PFT mappings");

            try
            {
                await ValidateAndCreateIndividuals(datasetId, quantity.IndividualPfts);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        else if (quantity.IndividualPfts != null)
        {
            return BadRequest("Non-individual-level data should not include PFT mappings");
        }

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

                case AggregationLevel.Individual:
                    foreach (var point in layerData.Data)
                    {
                        var datum = new IndividualDatum
                        {
                            Variable = variable,
                            Layer = layer,
                            Timestamp = point.Timestamp,
                            Longitude = point.Longitude,
                            Latitude = point.Latitude,
                            StandId = point.Stand ?? throw new ArgumentException("Stand ID is required"),
                            PatchId = point.Patch ?? throw new ArgumentException("Patch ID is required"),
                            IndividualId = point.Individual ?? throw new ArgumentException("Individual ID is required"),
                            Value = point.Value
                        };
                        _dbContext.IndividualData.Add(datum);
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown variable level: {variable.Level}");
            }
        }

        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Validates PFT mappings against existing database records and creates new PFTs and Individuals as needed.
    /// </summary>
    /// <param name="datasetId">The dataset ID.</param>
    /// <param name="mappings">The individual-PFT mappings to validate and create.</param>
    /// <exception cref="InvalidOperationException">Thrown if mappings are inconsistent with database.</exception>
    private async Task ValidateAndCreateIndividuals(int datasetId, IReadOnlyDictionary<int, string> mappings)
    {
        // Get existing individuals and their PFTs for this dataset
        var existingMappings = await _dbContext.Individuals
            .Where(i => i.DatasetId == datasetId)
            .Select(i => new { i.Number, i.Pft.Name })
            .ToDictionaryAsync(x => x.Number, x => x.Name);

        // Check for inconsistencies with existing mappings
        foreach (var (indivNumber, pftName) in mappings)
        {
            if (existingMappings.TryGetValue(indivNumber, out var existingPft) && existingPft != pftName)
            {
                throw new InvalidOperationException(
                    $"Inconsistent PFT mapping: Individual {indivNumber} is mapped to '{pftName}' " +
                    $"but exists in database with PFT '{existingPft}'");
            }
        }

        // Get or create PFTs
        var pftMap = new Dictionary<string, Pft>();
        foreach (var pftName in mappings.Values.Distinct())
        {
            var pft = await _dbContext.Pfts.FirstOrDefaultAsync(p => p.Name == pftName);
            if (pft == null)
            {
                pft = new Pft { Name = pftName };
                _dbContext.Pfts.Add(pft);
            }
            pftMap[pftName] = pft;
        }

        // Create new individuals (only for those not already in database)
        var newIndivs = mappings
            .Where(kvp => !existingMappings.ContainsKey(kvp.Key))
            .Select(kvp => new Individual
            {
                DatasetId = datasetId,
                Number = kvp.Key,
                Pft = pftMap[kvp.Value]
            });

        _dbContext.Individuals.AddRange(newIndivs);
        await _dbContext.SaveChangesAsync();
    }
}
