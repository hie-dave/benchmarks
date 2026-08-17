using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Web.Models;
using LpjGuess.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ObservationCurator)]
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

    [HttpPost("groups")]
    public async Task<ActionResult<int>> CreateGroup(
        [FromBody] CreateObservationGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Kind is not DatasetGroupKind.ObservationSite and not DatasetGroupKind.ObservationGridded)
            return BadRequest("Observation groups must be site-level or gridded");
        if (string.IsNullOrWhiteSpace(request.Source) || string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Version))
            return BadRequest("Source, name and version are required");

        string source = request.Source.Trim();
        string name = request.Name.Trim();
        string version = request.Version.Trim();
        try { JsonDocument.Parse(request.Metadata); }
        catch (JsonException) { return BadRequest("Metadata must be valid JSON"); }

        bool exists = await _dbContext.DatasetGroups.AnyAsync(g =>
            g.Source == source && g.Name == name && g.Version == version,
            cancellationToken);
        if (exists)
            return Conflict($"Observation release {request.Source}/{request.Name}/{request.Version} already exists");

        DatasetGroup group = new()
        {
            Name = name,
            Source = source,
            Version = version,
            Description = request.Description,
            Metadata = request.Metadata,
            Kind = request.Kind,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.DatasetGroups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(group.Id);
    }

    [HttpPost("groups/{groupId}/complete")]
    public async Task<ActionResult> CompleteGroup(int groupId, CancellationToken cancellationToken)
    {
        DatasetGroup? group = await _dbContext.DatasetGroups
            .Include(g => g.Datasets).ThenInclude(d => d.Variables).ThenInclude(v => v.Layers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group == null) return NotFound();
        if (group.Kind == DatasetGroupKind.Prediction) return BadRequest("Group is not an observation release");
        if (group.IsComplete) return BadRequest("Observation release is already complete");
        if (group.Datasets.Count == 0) return BadRequest("Observation release must contain at least one dataset");

        if (group.Datasets.Any(d => d is not ObservationDataset))
            return BadRequest("Observation releases may contain only observation datasets");

        foreach (ObservationDataset dataset in group.Datasets.OfType<ObservationDataset>())
        {
            if (group.Kind == DatasetGroupKind.ObservationSite && dataset.MatchingStrategy != MatchingStrategy.ByName)
                return BadRequest("Site observation datasets must use ByName matching");
            if (group.Kind == DatasetGroupKind.ObservationGridded && dataset.MatchingStrategy == MatchingStrategy.ByName)
                return BadRequest("Gridded observation datasets cannot use ByName matching");
            if (dataset.Variables.Count == 0) return BadRequest($"Dataset {dataset.Id} contains no variables");
            foreach (Variable variable in dataset.Variables)
            {
                if (string.IsNullOrWhiteSpace(variable.Units)) return BadRequest($"Variable {variable.Id} has no units");
                if (variable.Layers.Count == 0) return BadRequest($"Variable {variable.Id} contains no layers");
                foreach (VariableLayer layer in variable.Layers)
                {
                    bool hasData = await _dbContext.Set<Datum>()
                        .AnyAsync(d => d.VariableId == variable.Id && d.LayerId == layer.Id, cancellationToken);
                    if (!hasData) return BadRequest($"Layer {layer.Id} contains no data");
                }
            }
        }

        if (group.Kind == DatasetGroupKind.ObservationSite &&
            group.Datasets.GroupBy(d => d.SimulationId, StringComparer.Ordinal).Any(g => g.Count() > 1))
            return BadRequest("Site names must be unique within an observation release");

        group.IsComplete = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("groups/{groupId}/activate")]
    public async Task<ActionResult> ActivateGroup(int groupId, CancellationToken cancellationToken)
    {
        DatasetGroup? release = await _dbContext.DatasetGroups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (release == null) return NotFound();
        if (release.Kind == DatasetGroupKind.Prediction) return BadRequest("Group is not an observation release");
        if (!release.IsComplete) return BadRequest("Only complete observation releases can be activated");
        if (release.IsActive) return BadRequest("Observation release is already active");

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // A retry may reuse this DbContext after a rolled-back or
            // ambiguously committed attempt, so always reload database state.
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            DatasetGroup group = await _dbContext.DatasetGroups.Include(g => g.Datasets)
                .SingleAsync(g => g.Id == groupId, cancellationToken);

            List<DatasetGroup> active = await _dbContext.DatasetGroups.Include(g => g.Datasets)
                .Where(g => g.Source == group.Source && g.Name == group.Name && g.IsActive)
                .ToListAsync(cancellationToken);
            foreach (DatasetGroup previous in active)
            {
                previous.IsActive = false;
                previous.ActiveCollectionKey = null;
                foreach (ObservationDataset dataset in previous.Datasets.OfType<ObservationDataset>())
                    dataset.Active = false;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            group.IsActive = true;
            group.ActiveCollectionKey = CreateActiveCollectionKey(group.Source, group.Name);
            foreach (ObservationDataset dataset in group.Datasets.OfType<ObservationDataset>()) dataset.Active = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
        return Ok();
    }

    [HttpPost("groups/{groupId}/deactivate")]
    public async Task<ActionResult> DeactivateGroup(int groupId, CancellationToken cancellationToken)
    {
        DatasetGroup? group = await _dbContext.DatasetGroups.Include(g => g.Datasets)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group == null) return NotFound();
        if (!group.IsActive) return BadRequest("Observation release is already inactive");
        group.IsActive = false;
        group.ActiveCollectionKey = null;
        foreach (ObservationDataset dataset in group.Datasets.OfType<ObservationDataset>()) dataset.Active = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpDelete("groups/{groupId}")]
    public async Task<ActionResult> DeleteGroup(int groupId, CancellationToken cancellationToken)
    {
        DatasetGroup? group = await _dbContext.DatasetGroups
            .Include(g => g.Datasets)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group == null) return NotFound();
        if (group.Kind == DatasetGroupKind.Prediction) return BadRequest("Group is not an observation release");
        if (group.IsActive) return Conflict("Active observation releases cannot be deleted");

        _dbContext.Datasets.RemoveRange(group.Datasets);
        _dbContext.DatasetGroups.Remove(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("datasets/create")]
    public async Task<ActionResult<int>> CreateDataset([FromBody] CreateObservationDatasetRequest request)
    {
        if (!request.GroupId.HasValue) return BadRequest("Observation datasets must belong to a release group");
        var group = await _dbContext.DatasetGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId.Value);

        if (group == null) return NotFound($"Group {request.GroupId.Value} not found");
        if (group.Kind == DatasetGroupKind.Prediction) return BadRequest("Group is not an observation release");
        if (group.IsComplete) return BadRequest($"Group {request.GroupId.Value} is complete and immutable");
        if (group.Kind == DatasetGroupKind.ObservationSite && request.Strategy != MatchingStrategy.ByName)
            return BadRequest("Site observation datasets must use ByName matching");
        if (group.Kind == DatasetGroupKind.ObservationGridded && request.Strategy == MatchingStrategy.ByName)
            return BadRequest("Gridded observation datasets cannot use ByName matching");


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
            Source = group.Source,
            Version = group.Version,
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
            .Include(d => d.Group)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null)
            return NotFound($"Dataset {datasetId} not found");

        if (dataset is not ObservationDataset)
            return BadRequest($"Dataset {datasetId} is not an observation dataset");
        if (dataset.Group?.IsComplete == true)
            return BadRequest("Completed observation releases are immutable");

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
            .ThenInclude(d => d.Group)
            .FirstOrDefaultAsync(v => v.Id == variableId);

        if (variable == null)
            return NotFound($"Variable {variableId} not found");

        if (variable.Dataset is not ObservationDataset)
            return BadRequest($"Variable {variableId} does not belong to an observation dataset");
        if (variable.Dataset.Group?.IsComplete == true)
            return BadRequest("Completed observation releases are immutable");

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
        [FromBody] AppendObservationDataRequest request)
    {
        var layer = await _dbContext.VariableLayers
            .Include(l => l.Variable)
            .ThenInclude(v => v.Dataset)
            .ThenInclude(d => d.Group)
            .FirstOrDefaultAsync(l => l.Id == layerId);

        if (layer == null)
            return NotFound($"Layer {layerId} not found");

        if (layer.Variable.Dataset is not ObservationDataset)
            return BadRequest($"Layer {layerId} does not belong to an observation dataset");
        if (layer.Variable.Dataset.Group?.IsComplete == true)
            return BadRequest("Completed observation releases are immutable");

        bool siteLevel = layer.Variable.Dataset.Group?.Kind == DatasetGroupKind.ObservationSite;
        if (siteLevel && request.DataPoints.Any(d => d.Latitude.HasValue || d.Longitude.HasValue))
            return BadRequest("Site observation data must be identified by dataset name, not coordinates");
        if (!siteLevel && request.DataPoints.Any(d => !d.Latitude.HasValue || !d.Longitude.HasValue))
            return BadRequest("Gridded observation data requires longitude and latitude");

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

    [NonAction]
    public Task<ActionResult> AppendData(int layerId, AppendDataRequest request) =>
        AppendData(layerId, new AppendObservationDataRequest
        {
            DataPoints = request.DataPoints.Select(d => new ObservationDataPoint(
                d.Timestamp, d.Value, d.Longitude, d.Latitude, d.Stand, d.Patch, d.Individual)).ToArray()
        });

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
    
        if (!dataset.GroupId.HasValue) return BadRequest("Observation dataset has no release group");
        return await ActivateGroup(dataset.GroupId.Value, CancellationToken.None);
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
    
        if (!dataset.GroupId.HasValue) return BadRequest("Observation dataset has no release group");
        return await DeactivateGroup(dataset.GroupId.Value, CancellationToken.None);
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

    private static string CreateActiveCollectionKey(string source, string name) => $"{source}\n{name}";
}
