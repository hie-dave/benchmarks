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

    [HttpPost("site/create")]
    public async Task<ActionResult<SiteRunDataset>> CreateSiteDataset(
        [FromBody] CreateSiteDatasetRequest request)
    {
        SiteRunDataset dataset = new()
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            ModelVersion = request.ModelVersion,
            ClimateDataset = request.ClimateDataset,
            TemporalResolution = request.TemporalResolution,
            Patches = request.CompressedCodePatches
        };

        _dbContext.Datasets.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(dataset);
    }

    [HttpPost("gridded/create")]
    public async Task<ActionResult<GriddedDataset>> CreateGriddedDataset(
        [FromBody] CreateGriddedDatasetRequest request)
    {
        GriddedDataset dataset = new()
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            ModelVersion = request.ModelVersion,
            ClimateDataset = request.ClimateDataset,
            TemporalResolution = request.TemporalResolution,
            SpatialExtent = request.SpatialExtent,
            SpatialResolution = request.SpatialResolution,
            Patches = request.CompressedCodePatches
        };

        _dbContext.Datasets.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(dataset);
    }

    [HttpPost("site/{datasetId}/add")]
    public async Task<ActionResult> AddSite(
        int datasetId,
        [FromBody] AddSiteRequest request)
    {
        logger.LogInformation(
            "Adding site {name} to dataset {DatasetId}",
            request.Name,
            datasetId);

        Dataset? dataset = await _dbContext.Datasets
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset is not SiteRunDataset siteDataset)
            return BadRequest("Dataset is not a site run dataset");

        SiteRun site = new()
        {
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DatasetId = datasetId,
        };
        site.SetCompressedInstructions(request.InstructionFile);

        _dbContext.SiteRuns.Add(site);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("gridded/{datasetId}/add")]
    public async Task<ActionResult> AddGriddedRun(
        int datasetId,
        [FromBody] AddGriddedRunRequest request)
    {
        logger.LogInformation(
            "Adding scenario {gcm}/{scenario} to dataset {DatasetId}",
            request.GcmName,
            request.EmissionsScenario,
            datasetId);

        Dataset? dataset = await _dbContext.Datasets
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset is not GriddedDataset griddedDataset)
            return BadRequest("Dataset is not a gridded dataset");

        ClimateScenario scenario = new()
        {
            GcmName = request.GcmName,
            EmissionsScenario = request.EmissionsScenario,
            DatasetId = datasetId,
        };
        scenario.SetCompressedInstructions(request.InstructionFile);

        _dbContext.ClimateScenarios.Add(scenario);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{datasetId}/add")]
    public async Task<ActionResult> AddQuantity(
        int datasetId,
        [FromBody] Quantity quantity)
    {
        logger.LogInformation(
            "Adding quantity {name} to dataset {DatasetId}",
            quantity.Name,
            datasetId);

        using var _ = logger.BeginScope($"add {quantity.Name}");

        Dataset? dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        // Handle individual-level data validation and creation
        if (quantity.Level == AggregationLevel.Individual)
        {
            if (quantity.IndividualPfts == null)
            {
                logger.LogInformation("Quantity is an individual-level output, but does not contain PFT mappings");
                return BadRequest("Individual-level data must include PFT mappings");
            }

            try
            {
                await ValidateAndCreateIndividuals(datasetId, quantity.IndividualPfts);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogInformation(ex, "Invalid request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
        else if (quantity.IndividualPfts != null)
        {
            logger.LogInformation("Quantity is not an individual-level output, but does include PFT mappings");
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
                    foreach (DataPoint point in layerData.Data)
                    {
                        GridcellDatum datum = new()
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
                    foreach (DataPoint point in layerData.Data)
                    {
                        StandDatum datum = new()
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
                    foreach (DataPoint point in layerData.Data)
                    {
                        PatchDatum datum = new()
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
                    foreach (DataPoint point in layerData.Data)
                    {
                        IndividualDatum datum = new()
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
        logger.LogDebug("Successfully added quantity {name} to dataset {DatasetId}", quantity.Name, datasetId);
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
            if (existingMappings.TryGetValue(indivNumber, out string? existingPft) && existingPft != pftName)
            {
                throw new InvalidOperationException(
                    $"Inconsistent PFT mapping: Individual {indivNumber} is mapped to '{pftName}' " +
                    $"but exists in database with PFT '{existingPft}'");
            }
        }

        // Get or create PFTs
        Dictionary<string, Pft> pftMap = new();
        foreach (string pftName in mappings.Values.Distinct())
        {
            Pft? pft = await _dbContext.Pfts.FirstOrDefaultAsync(p => p.Name == pftName);
            if (pft == null)
            {
                pft = new Pft { Name = pftName };
                _dbContext.Pfts.Add(pft);
            }
            pftMap[pftName] = pft;
        }

        // Create new individuals (only for those not already in database)
        IEnumerable<Individual> newIndivs = mappings
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
