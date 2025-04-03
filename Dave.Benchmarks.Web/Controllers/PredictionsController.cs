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

    [HttpPost("groups/create")]
    public async Task<ActionResult<int>> CreateDatasetGroup(
        [FromBody] CreateDatasetGroupRequest request)
    {
        var group = new DatasetGroup
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            Metadata = request.Metadata
        };

        _dbContext.DatasetGroups.Add(group);
        await _dbContext.SaveChangesAsync();

        return Ok(group.Id);
    }

    [HttpPost("groups/{groupId}/complete")]
    public async Task<ActionResult> CompleteDatasetGroup(int groupId)
    {
        var group = await _dbContext.DatasetGroups
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return NotFound($"Group {groupId} not found");

        group.IsComplete = true;
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("create")]
    public async Task<ActionResult<int>> CreateDataset(
        [FromBody] CreateDatasetRequest request)
    {
        // If a group ID is provided, verify it exists and is not complete
        if (request.GroupId.HasValue)
        {
            var group = await _dbContext.DatasetGroups
                .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value);

            if (group == null)
                return NotFound($"Group {request.GroupId.Value} not found");

            if (group.IsComplete)
                return BadRequest($"Group {request.GroupId.Value} is marked as complete and cannot accept new datasets");
        }

        var dataset = new PredictionDataset
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            ModelVersion = request.ModelVersion,
            ClimateDataset = request.ClimateDataset,
            TemporalResolution = request.TemporalResolution,
            Patches = request.CompressedCodePatches,
            Metadata = request.Metadata,
            GroupId = request.GroupId
        };

        _dbContext.Datasets.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(dataset.Id);
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
            throw new InvalidOperationException("All layers must have the same unit");

        if (variable == null)
        {
            // Create new variable
            variable = new Variable
            {
                Name = quantity.Name,
                Description = quantity.Description,
                Units = quantity.Layers.First().Unit.Name,
                DatasetId = datasetId,
                Level = quantity.Level
            };

            dataset.Variables.Add(variable);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Validate variable matches
            if (variable.Units != quantity.Layers.First().Unit.Name)
                throw new InvalidOperationException(
                    $"Variable {quantity.Name} already exists with unit " +
                    $"{variable.Units}, but request has unit {quantity.Layers.First().Unit.Name}");

            if (variable.Level != quantity.Level)
                throw new InvalidOperationException(
                    $"Variable {quantity.Name} already exists with level " +
                    $"{variable.Level}, but request has level {quantity.Level}");
        }

        // Create layers and their data points
        foreach (var layer in quantity.Layers)
        {
            var variableLayer = new VariableLayer
            {
                Name = layer.Name,
                Description = layer.Name, // TODO: add layer-level descriptions?
                Variable = variable  // Use navigation property instead of ID
            };

            _dbContext.VariableLayers.Add(variableLayer);
            await _dbContext.SaveChangesAsync();

            // Add data points for this layer
            switch (quantity.Level)
            {
                case AggregationLevel.Gridcell:
                    var gridcellData = layer.Data.Select(d => new GridcellDatum
                    {
                        Timestamp = d.Timestamp,
                        Value = d.Value,
                        Latitude = d.Latitude,
                        Longitude = d.Longitude,
                        Variable = variable,
                        Layer = variableLayer
                    });
                    _dbContext.GridcellData.AddRange(gridcellData);
                    break;

                case AggregationLevel.Stand:
                    var standData = layer.Data.Select(d => new StandDatum
                    {
                        Timestamp = d.Timestamp,
                        Value = d.Value,
                        Latitude = d.Latitude,
                        Longitude = d.Longitude,
                        StandId = d.Stand ?? throw new InvalidOperationException("Stand data must include stand ID"),
                        Variable = variable,
                        Layer = variableLayer
                    });
                    _dbContext.StandData.AddRange(standData);
                    break;

                case AggregationLevel.Patch:
                    var patchData = layer.Data.Select(d => new PatchDatum
                    {
                        Timestamp = d.Timestamp,
                        Value = d.Value,
                        Latitude = d.Latitude,
                        Longitude = d.Longitude,
                        StandId = d.Stand ?? throw new InvalidOperationException("Patch data must include stand ID"),
                        PatchId = d.Patch ?? throw new InvalidOperationException("Patch data must include patch ID"),
                        Variable = variable,
                        Layer = variableLayer
                    });
                    _dbContext.PatchData.AddRange(patchData);
                    break;

                case AggregationLevel.Individual:
                    var individualData = layer.Data.Select(d => new IndividualDatum
                    {
                        Timestamp = d.Timestamp,
                        Value = d.Value,
                        Latitude = d.Latitude,
                        Longitude = d.Longitude,
                        StandId = d.Stand ?? throw new InvalidOperationException("Individual data must include stand ID"),
                        PatchId = d.Patch ?? throw new InvalidOperationException("Individual data must include patch ID"),
                        Individual = _dbContext.Individuals.First(i => i.DatasetId == datasetId && i.Number == d.Individual!),
                        Variable = variable,
                        Layer = variableLayer
                    });
                    _dbContext.IndividualData.AddRange(individualData);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown aggregation level: {quantity.Level}");
            }

            await _dbContext.SaveChangesAsync();
            logger.LogInformation("Added {count} data points for layer {layer}", layer.Data.Count, layer.Name);
        }

        return Ok();
    }

    [HttpPost("{datasetId}/variables")]
    public async Task<ActionResult<int>> CreateVariable(
        int datasetId,
        [FromBody] CreateVariableRequest request)
    {
        logger.LogInformation(
            "Creating variable {name} in dataset {DatasetId}",
            request.Name,
            datasetId);

        Dataset? dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        // Handle individual-level data validation and creation
        if (request.Level == AggregationLevel.Individual)
        {
            if (request.IndividualPfts == null)
            {
                logger.LogInformation("Variable is an individual-level output, but does not contain PFT mappings");
                return BadRequest("Individual-level data must include PFT mappings");
            }

            try
            {
                await ValidateAndCreateIndividuals(datasetId, request.IndividualPfts);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogInformation(ex, "Invalid request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
        else if (request.IndividualPfts != null)
        {
            logger.LogInformation("Variable is not an individual-level output, but does include PFT mappings");
            return BadRequest("Non-individual-level data should not include PFT mappings");
        }

        // Check if variable already exists
        Variable? variable = dataset.Variables
            .FirstOrDefault(v => v.Name == request.Name);

        if (variable != null)
        {
            // Validate variable matches
            if (variable.Units != request.Units)
                return BadRequest(
                    $"Variable {request.Name} already exists with unit " +
                    $"{variable.Units}, but request has unit {request.Units}");

            if (variable.Level != request.Level)
                return BadRequest(
                    $"Variable {request.Name} already exists with level " +
                    $"{variable.Level}, but request has level {request.Level}");

            return Ok(variable.Id);
        }

        // Create new variable
        variable = new Variable
        {
            Name = request.Name,
            Description = request.Description,
            Units = request.Units,
            DatasetId = datasetId,
            Level = request.Level
        };

        dataset.Variables.Add(variable);
        await _dbContext.SaveChangesAsync();
        
        return Ok(variable.Id);
    }

    [HttpPost("variables/{variableId}/layers")]
    public async Task<ActionResult<int>> CreateLayer(
        int variableId,
        [FromBody] CreateLayerRequest request)
    {
        var variable = await _dbContext.Variables
            .FirstOrDefaultAsync(v => v.Id == variableId);

        if (variable == null)
            return NotFound($"Variable {variableId} not found");

        var layer = new VariableLayer
        {
            Name = request.Name,
            Description = request.Description,
            Variable = variable
        };

        _dbContext.VariableLayers.Add(layer);
        await _dbContext.SaveChangesAsync();

        return Ok(layer.Id);
    }

    [HttpPost("layers/{layerId}/data")]
    public async Task<ActionResult> AppendData(
        int layerId,
        [FromBody] AppendDataRequest request)
    {
        var layer = await _dbContext.VariableLayers
            .Include(l => l.Variable)
            .FirstOrDefaultAsync(l => l.Id == layerId);

        if (layer == null)
            return NotFound($"Layer {layerId} not found");

        // Add data points for this layer
        switch (layer.Variable.Level)
        {
            case AggregationLevel.Gridcell:
                var gridcellData = request.DataPoints.Select(d => new GridcellDatum
                {
                    Timestamp = d.Timestamp,
                    Value = d.Value,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    Variable = layer.Variable,
                    Layer = layer
                });
                _dbContext.GridcellData.AddRange(gridcellData);
                break;

            case AggregationLevel.Stand:
                var standData = request.DataPoints.Select(d => new StandDatum
                {
                    Timestamp = d.Timestamp,
                    Value = d.Value,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    StandId = d.Stand ?? throw new InvalidOperationException("Stand data must include stand ID"),
                    Variable = layer.Variable,
                    Layer = layer
                });
                _dbContext.StandData.AddRange(standData);
                break;

            case AggregationLevel.Patch:
                var patchData = request.DataPoints.Select(d => new PatchDatum
                {
                    Timestamp = d.Timestamp,
                    Value = d.Value,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    StandId = d.Stand ?? throw new InvalidOperationException("Patch data must include stand ID"),
                    PatchId = d.Patch ?? throw new InvalidOperationException("Patch data must include patch ID"),
                    Variable = layer.Variable,
                    Layer = layer
                });
                _dbContext.PatchData.AddRange(patchData);
                break;

            case AggregationLevel.Individual:
                var individualData = request.DataPoints.Select(d => new IndividualDatum
                {
                    Timestamp = d.Timestamp,
                    Value = d.Value,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    StandId = d.Stand ?? throw new InvalidOperationException("Individual data must include stand ID"),
                    PatchId = d.Patch ?? throw new InvalidOperationException("Individual data must include patch ID"),
                    Individual = _dbContext.Individuals.First(i => 
                        i.DatasetId == layer.Variable.DatasetId && 
                        i.Number == d.Individual!),
                    Variable = layer.Variable,
                    Layer = layer
                });
                _dbContext.IndividualData.AddRange(individualData);
                break;

            default:
                throw new ArgumentException($"Unknown aggregation level: {layer.Variable.Level}");
        }

        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    private async Task ValidateAndCreateIndividuals(
        int datasetId,
        IReadOnlyDictionary<int, string> mappings)
    {
        // Get existing individuals for this dataset
        var existingIndividuals = await _dbContext.Individuals
            .Where(i => i.DatasetId == datasetId)
            .ToListAsync();

        // Get all PFTs
        var pfts = await _dbContext.Pfts.ToListAsync();

        foreach ((int indivId, string pftName) in mappings)
        {
            // Check if individual already exists
            var existing = existingIndividuals
                .FirstOrDefault(i => i.Number == indivId);

            if (existing != null)
            {
                // Validate PFT matches
                if (existing.Pft.Name != pftName)
                {
                    throw new InvalidOperationException(
                        $"Individual {indivId} already exists with PFT " +
                        $"'{existing.Pft.Name}', but request has PFT '{pftName}'");
                }

                continue;
            }

            // Get or create PFT
            var pft = pfts.FirstOrDefault(p => p.Name == pftName);
            if (pft == null)
            {
                pft = new Pft { Name = pftName };
                _dbContext.Pfts.Add(pft);
                pfts.Add(pft);
            }

            // Create individual
            var individual = new Individual
            {
                Number = indivId,
                DatasetId = datasetId,
                Pft = pft
            };

            _dbContext.Individuals.Add(individual);
            existingIndividuals.Add(individual);
        }
    }
}
