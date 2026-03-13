using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Web.Models;
using LpjGuess.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ObservationsController : ControllerBase
{
    private readonly BenchmarksDbContext _dbContext;
    private readonly ILogger<ObservationsController> _logger;

    public ObservationsController(
        BenchmarksDbContext dbContext,
        ILogger<ObservationsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("datasets/create")]
    public async Task<ActionResult<int>> CreateDataset([FromBody] CreateObservationDatasetRequest request)
    {
        if (request.GroupId.HasValue)
        {
            var group = await _dbContext.DatasetGroups
                .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value);

            if (group == null)
                return NotFound($"Group {request.GroupId.Value} not found");

            if (group.IsComplete)
                return BadRequest($"Group {request.GroupId.Value} is marked as complete and cannot accept new datasets");
        }

        if (request.Strategy == MatchingStrategy.Nearest && !request.MaxDistance.HasValue)
            return BadRequest("MaxDistance is required when using the Nearest matching strategy");

        if (request.Strategy != MatchingStrategy.Nearest && request.MaxDistance.HasValue)
            return BadRequest("MaxDistance should only be provided when using the Nearest matching strategy");

        if (request.Strategy == MatchingStrategy.ByName && string.IsNullOrWhiteSpace(request.SimulationId))
            return BadRequest("SimulationId is required when using the ByName matching strategy");

        // Setting SimulationID when not using ByName is allowed for now, even
        // though it's never used for matching, because we may later add API to
        // change the matching strategy of an existing dataset.

        var dataset = new ObservationDataset
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            Source = request.Source,
            Version = request.Version,
            SpatialResolution = request.SpatialResolution,
            TemporalResolution = request.TemporalResolution,
            Metadata = request.Metadata,
            GroupId = request.GroupId,
            SimulationId = request.SimulationId,
            MatchingStrategy = request.Strategy,
            MaxDistance = request.MaxDistance
        };

        _dbContext.Datasets.Add(dataset);
        await _dbContext.SaveChangesAsync();

        return Ok(dataset.Id);
    }

    [HttpPost("{datasetId}/variables")]
    public async Task<ActionResult<int>> CreateVariable(
        int datasetId,
        [FromBody] CreateVariableRequest request)
    {
        _logger.LogInformation(
            "Creating variable {name} in observation dataset {DatasetId}",
            request.Name,
            datasetId);

        Dataset? dataset = await _dbContext.Datasets
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        if (dataset is not ObservationDataset)
            return BadRequest($"Dataset {datasetId} is not an observation dataset");

        if (request.Level == AggregationLevel.Individual)
        {
            // TODO: test this. Low priority since indiv-level observations will
            // probably never be used.
            if (request.IndividualPfts == null)
                return BadRequest("Individual-level data must include PFT mappings");

            try
            {
                await ValidateAndCreateIndividuals(datasetId, request.IndividualPfts);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation(ex, "Invalid request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
        else if (request.IndividualPfts != null)
        {
            return BadRequest("Non-individual-level data should not include PFT mappings");
        }

        Variable? variable = dataset.Variables
            .FirstOrDefault(v => v.Name == request.Name &&
                            v.Level == request.Level &&
                            v.Description == request.Description &&
                            v.Units == request.Units);

        if (variable != null)
            return Ok(variable.Id);

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
            .Include(v => v.Dataset)
            .FirstOrDefaultAsync(v => v.Id == variableId);

        if (variable == null)
            return NotFound($"Variable {variableId} not found");

        if (variable.Dataset is not ObservationDataset)
            return BadRequest($"Variable {variableId} does not belong to an observation dataset");

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
            .ThenInclude(v => v.Dataset)
            .FirstOrDefaultAsync(l => l.Id == layerId);

        if (layer == null)
            return NotFound($"Layer {layerId} not found");

        if (layer.Variable.Dataset is not ObservationDataset)
            return BadRequest($"Layer {layerId} does not belong to an observation dataset");

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

    /// <summary>
    /// Activate an observation dataset for use in the evaluation API. This will
    /// cause this observed dataset to be used for comparisons with predictions
    /// for evaluation purposes.
    /// </summary>
    /// <param name="datasetId">ID of the observation dataset to activate.</param>
    [HttpPost("datasets/{datasetId}/activate")]
    public async Task<ActionResult> ActivateDataset(int datasetId)
    {
        ObservationDataset? dataset = await _dbContext.Datasets
            .OfType<ObservationDataset>()
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Observation dataset {datasetId} not found");
    
        if (dataset.Active)
            return BadRequest($"Observation dataset {datasetId} is already active");

        dataset.Active = true;
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Deactivate an observation dataset, preventing it from being used for
    /// evaluation. This does not delete the dataset or its data, but simply
    /// marks it as inactive so that it won't be used for comparisons with
    /// predictions for evaluation purposes.
    /// </summary>
    /// <param name="datasetId">ID of the observation dataset to deactivate.</param>
    [HttpPost("datasets/{datasetId}/deactivate")]
    public async Task<ActionResult> DeactivateDataset(int datasetId)
    {
        ObservationDataset? dataset = await _dbContext.Datasets
            .OfType<ObservationDataset>()
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Observation dataset {datasetId} not found");
    
        if (!dataset.Active)
            return BadRequest($"Observation dataset {datasetId} is already inactive");

        dataset.Active = false;
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    private async Task ValidateAndCreateIndividuals(
        int datasetId,
        IReadOnlyDictionary<int, string> mappings)
    {
        var existingIndividuals = await _dbContext.Individuals
            .Where(i => i.DatasetId == datasetId)
            .ToListAsync();

        var pfts = await _dbContext.Pfts.ToListAsync();

        foreach ((int indivId, string pftName) in mappings)
        {
            var existing = existingIndividuals
                .FirstOrDefault(i => i.Number == indivId);

            if (existing != null)
            {
                if (existing.Pft.Name != pftName)
                {
                    throw new InvalidOperationException(
                        $"Individual {indivId} already exists with PFT " +
                        $"'{existing.Pft.Name}', but request has PFT '{pftName}'");
                }

                continue;
            }

            var pft = pfts.FirstOrDefault(p => p.Name == pftName);
            if (pft == null)
            {
                pft = new Pft { Name = pftName };
                _dbContext.Pfts.Add(pft);
                pfts.Add(pft);
            }

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
