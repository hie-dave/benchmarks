using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Tests.Helpers;
using Dave.Benchmarks.Web.Controllers;
using Dave.Benchmarks.Web.Models;
using LpjGuess.Core.Models.Entities;
using LpjGuess.Core.Models.Importer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class ObservationsControllerTests
{
    [Fact]
    public async Task CreateDataset_WhenGroupMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        CreateObservationDatasetRequest request = BaseCreateObservationRequest();
        request.GroupId = 999;
        request.Strategy = MatchingStrategy.ExactMatch;

        ActionResult<int> result = await controller.CreateDataset(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDataset_WhenGroupComplete_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DatasetGroup group = new()
        {
            Name = "g1",
            Description = "desc",
            CreatedAt = DateTime.UtcNow,
            IsComplete = true,
            Metadata = "{}"
        };
        db.DatasetGroups.Add(group);
        db.SaveChanges();

        ObservationsController controller = CreateController(db);
        CreateObservationDatasetRequest request = BaseCreateObservationRequest();
        request.GroupId = group.Id;
        request.Strategy = MatchingStrategy.ExactMatch;

        ActionResult<int> result = await controller.CreateDataset(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDataset_WhenNearestWithoutMaxDistance_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        CreateObservationDatasetRequest request = BaseCreateObservationRequest();
        request.Strategy = MatchingStrategy.Nearest;
        request.MaxDistance = null;

        ActionResult<int> result = await controller.CreateDataset(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDataset_WhenNonNearestWithMaxDistance_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        CreateObservationDatasetRequest request = BaseCreateObservationRequest();
        request.Strategy = MatchingStrategy.ExactMatch;
        request.MaxDistance = 20;

        ActionResult<int> result = await controller.CreateDataset(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDataset_WhenByNameWithoutSimulationId_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        CreateObservationDatasetRequest request = BaseCreateObservationRequest();
        request.Strategy = MatchingStrategy.ByName;
        request.SimulationId = "";

        ActionResult<int> result = await controller.CreateDataset(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDataset_WithNearestAndDistance_CreatesDataset()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);
        DatasetGroup group = AddObservationGroup(db, DatasetGroupKind.ObservationGridded);

        CreateObservationDatasetRequest request = BaseCreateObservationRequest();
        request.GroupId = group.Id;
        request.Strategy = MatchingStrategy.Nearest;
        request.MaxDistance = 25;

        ActionResult<int> result = await controller.CreateDataset(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int id = Assert.IsType<int>(ok.Value);
        ObservationDataset created = db.Datasets.OfType<ObservationDataset>().Single(d => d.Id == id);
        Assert.Equal(MatchingStrategy.Nearest, created.MatchingStrategy);
        Assert.Equal(25, created.MaxDistance);
    }

    [Fact]
    public async Task CreateDataset_WhenGroupNotComplete_AssignsDatasetToGroup()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DatasetGroup group = new()
        {
            Name = "g-open",
            Description = "desc",
            CreatedAt = DateTime.UtcNow,
            IsComplete = false,
            Metadata = "{}",
            Kind = DatasetGroupKind.ObservationGridded,
            Source = "source",
            Version = "v1"
        };
        db.DatasetGroups.Add(group);
        db.SaveChanges();

        ObservationsController controller = CreateController(db);
        CreateObservationDatasetRequest request = BaseCreateObservationRequest();
        request.GroupId = group.Id;
        request.Strategy = MatchingStrategy.ExactMatch;

        ActionResult<int> result = await controller.CreateDataset(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int datasetId = Assert.IsType<int>(ok.Value);
        ObservationDataset created = db.Datasets.OfType<ObservationDataset>().Single(d => d.Id == datasetId);
        Assert.Equal(group.Id, created.GroupId);
    }

    [Fact]
    public async Task CreateVariable_WhenDatasetMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(999, new CreateVariableRequest
        {
            Name = "lai",
            Description = "lai",
            Level = AggregationLevel.Gridcell,
            Units = "m2m2"
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateVariable_WhenDatasetIsNotObservation_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset prediction = EvaluationSeed.CreatePredictionDataset(db);
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(prediction.Id, new CreateVariableRequest
        {
            Name = "lai",
            Description = "lai",
            Level = AggregationLevel.Gridcell,
            Units = "m2m2"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateVariable_WhenIndividualWithoutPfts_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "fpc",
            Description = "fpc",
            Level = AggregationLevel.Individual,
            Units = "frac"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateVariable_WhenNonIndividualHasPfts_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "lai",
            Description = "lai",
            Level = AggregationLevel.Gridcell,
            Units = "m2m2",
            IndividualPfts = new Dictionary<int, string> { [1] = "Tree" }
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateVariable_WhenExisting_ReturnsExistingId()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        (Variable variable, _) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Gridcell, variableName: "lai", units: "m2m2");
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "lai",
            Description = "lai",
            Level = AggregationLevel.Gridcell,
            Units = "m2m2"
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(variable.Id, Assert.IsType<int>(ok.Value));
    }

    [Fact]
    public async Task CreateVariable_IndividualWithPfts_CreatesVariableAndIndividuals()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "fpc",
            Description = "fpc",
            Level = AggregationLevel.Individual,
            Units = "frac",
            IndividualPfts = new Dictionary<int, string> { [7] = "Tree" }
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int variableId = Assert.IsType<int>(ok.Value);
        Assert.True(db.Variables.Any(v => v.Id == variableId));
        Assert.True(db.Individuals.Any(i => i.DatasetId == dataset.Id && i.Number == 7));
    }

    [Fact]
    public async Task CreateVariable_IndividualWithConflictingPft_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        EvaluationSeed.EnsureIndividual(db, dataset, individualNumber: 7, pftName: "Tree");
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "fpc",
            Description = "fpc",
            Level = AggregationLevel.Individual,
            Units = "frac",
            IndividualPfts = new Dictionary<int, string> { [7] = "Grass" }
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_WhenVariableMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateLayer(999, new CreateLayerRequest
        {
            Name = "layer-a",
            Description = "layer-a"
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_WhenVariableNotObservation_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset prediction = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, _) = EvaluationSeed.AddVariableLayer(db, prediction);
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateLayer(variable.Id, new CreateLayerRequest
        {
            Name = "layer-a",
            Description = "layer-a"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_WhenValid_ReturnsLayerId()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        (Variable variable, _) = EvaluationSeed.AddVariableLayer(db, dataset, variableName: "lai", layerName: "old");
        ObservationsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateLayer(variable.Id, new CreateLayerRequest
        {
            Name = "new",
            Description = "new"
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int layerId = Assert.IsType<int>(ok.Value);
        Assert.True(db.VariableLayers.Any(l => l.Id == layerId && l.VariableId == variable.Id));
    }

    [Fact]
    public async Task AppendData_WhenLayerMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(999, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)]
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AppendData_WhenLayerNotObservation_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset prediction = EvaluationSeed.CreatePredictionDataset(db);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, prediction);
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)]
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AppendData_Gridcell_AppendsData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Gridcell);
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)]
        });

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.GridcellData.Count(d => d.LayerId == layer.Id));
    }

    [Fact]
    public async Task AppendData_Stand_AppendsData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Stand);
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0, Stand: 1)]
        });

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.StandData.Count(d => d.LayerId == layer.Id));
    }

    [Fact]
    public async Task AppendData_PatchWithoutPatchId_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Patch);
        ObservationsController controller = CreateController(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0, Stand: 1)]
        }));
    }

    [Fact]
    public async Task AppendData_Individual_AppendsData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset dataset = EvaluationSeed.CreateObservationDataset(db, active: false);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Individual);
        EvaluationSeed.EnsureIndividual(db, dataset, individualNumber: 9, pftName: "Tree");
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 0.3, Stand: 1, Patch: 1, Individual: 9)]
        });

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.IndividualData.Count(d => d.LayerId == layer.Id));
    }

    [Fact]
    public async Task ActivateDataset_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.ActivateDataset(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ActivateDataset_WhenAlreadyActive_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, active: true);
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.ActivateDataset(obs.Id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ActivateDataset_WhenInactive_Activates()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, active: false, complete: true);
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.ActivateDataset(obs.Id);

        Assert.IsType<OkResult>(result);
        Assert.True(db.Datasets.OfType<ObservationDataset>().Single(d => d.Id == obs.Id).Active);
    }

    [Fact]
    public async Task DeactivateDataset_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.DeactivateDataset(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateDataset_WhenAlreadyInactive_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, active: false, complete: true);
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.DeactivateDataset(obs.Id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateDataset_WhenActive_Deactivates()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset obs = EvaluationSeed.CreateObservationDataset(db, active: true);
        ObservationsController controller = CreateController(db);

        ActionResult result = await controller.DeactivateDataset(obs.Id);

        Assert.IsType<OkResult>(result);
        Assert.False(db.Datasets.OfType<ObservationDataset>().Single(d => d.Id == obs.Id).Active);
    }

    [Fact]
    public async Task DeleteGroup_WhenInactive_RemovesReleaseAndDatasets()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset observation = EvaluationSeed.CreateObservationDataset(db, active: false);
        int groupId = observation.GroupId!.Value;

        ActionResult result = await CreateController(db).DeleteGroup(groupId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.DatasetGroups.Any(g => g.Id == groupId));
        Assert.False(db.Datasets.Any(d => d.Id == observation.Id));
    }

    [Fact]
    public async Task DeleteGroup_WhenActive_ReturnsConflict()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset observation = EvaluationSeed.CreateObservationDataset(db, active: true);

        ActionResult result = await CreateController(db)
            .DeleteGroup(observation.GroupId!.Value, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.True(db.DatasetGroups.Any(g => g.Id == observation.GroupId));
    }

    [Fact]
    public async Task ActivateGroup_ReplacesActiveVersionOfSameCollection()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DatasetGroup oldRelease = AddObservationGroup(db, DatasetGroupKind.ObservationSite);
        oldRelease.Name = "ozflux";
        oldRelease.Version = "2025";
        oldRelease.IsComplete = true;
        oldRelease.IsActive = true;
        oldRelease.ActiveCollectionKey = $"{oldRelease.Source}\n{oldRelease.Name}";
        ObservationDataset oldDataset = AddSiteDataset(db, oldRelease, "AU-Tum", active: true);

        DatasetGroup newRelease = AddObservationGroup(db, DatasetGroupKind.ObservationSite);
        newRelease.Name = oldRelease.Name;
        newRelease.Version = "2026";
        newRelease.IsComplete = true;
        ObservationDataset newDataset = AddSiteDataset(db, newRelease, "AU-Tum", active: false);
        db.SaveChanges();

        ActionResult result = await CreateController(db).ActivateGroup(newRelease.Id, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        DatasetGroup persistedOldRelease = db.DatasetGroups.Single(g => g.Id == oldRelease.Id);
        DatasetGroup persistedNewRelease = db.DatasetGroups.Single(g => g.Id == newRelease.Id);
        Assert.False(persistedOldRelease.IsActive);
        Assert.Null(persistedOldRelease.ActiveCollectionKey);
        Assert.False(db.Datasets.OfType<ObservationDataset>().Single(d => d.Id == oldDataset.Id).Active);
        Assert.True(persistedNewRelease.IsActive);
        Assert.Equal($"{newRelease.Source}\n{newRelease.Name}", persistedNewRelease.ActiveCollectionKey);
        Assert.True(db.Datasets.OfType<ObservationDataset>().Single(d => d.Id == newDataset.Id).Active);
    }

    [Fact]
    public void ActiveCollectionKey_AllowsOnlyOneActiveVersion()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        const string key = "source\nozflux";
        db.DatasetGroups.AddRange(
            new DatasetGroup
            {
                Name = "ozflux", Source = "source", Version = "1", Description = "one",
                CreatedAt = DateTime.UtcNow, Kind = DatasetGroupKind.ObservationSite,
                IsComplete = true, IsActive = true, ActiveCollectionKey = key
            },
            new DatasetGroup
            {
                Name = "ozflux", Source = "source", Version = "2", Description = "two",
                CreatedAt = DateTime.UtcNow, Kind = DatasetGroupKind.ObservationSite,
                IsComplete = true, IsActive = true, ActiveCollectionKey = key
            });

        Assert.Throws<Microsoft.EntityFrameworkCore.DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public async Task CreateGroup_WithInvalidMetadata_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        CreateObservationGroupRequest request = new()
        {
            Name = "ozflux", Source = "source", Version = "1",
            Kind = DatasetGroupKind.ObservationSite, Metadata = "not-json"
        };

        ActionResult<int> result = await CreateController(db).CreateGroup(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.DatasetGroups);
    }

    private static ObservationsController CreateController(BenchmarksDbContext db)
    {
        return new ObservationsController(db, Mock.Of<ILogger<ObservationsController>>());
    }

    private static CreateObservationDatasetRequest BaseCreateObservationRequest()
    {
        return new CreateObservationDatasetRequest
        {
            Name = "obs",
            Description = "obs",
            Source = "src",
            Version = "v1",
            SpatialResolution = "site",
            TemporalResolution = "daily",
            Metadata = "{}"
        };
    }

    private static DatasetGroup AddObservationGroup(BenchmarksDbContext db, DatasetGroupKind kind)
    {
        DatasetGroup group = new()
        {
            Name = $"group-{Guid.NewGuid():N}", Source = "source", Version = "v1",
            Description = "test", CreatedAt = DateTime.UtcNow, Kind = kind, Metadata = "{}"
        };
        db.DatasetGroups.Add(group);
        db.SaveChanges();
        return group;
    }

    private static ObservationDataset AddSiteDataset(
        BenchmarksDbContext db, DatasetGroup group, string site, bool active)
    {
        ObservationDataset dataset = new()
        {
            Name = site, Description = site, CreatedAt = DateTime.UtcNow,
            Source = group.Source, Version = group.Version, SpatialResolution = "site",
            TemporalResolution = "daily", Metadata = "{}", SimulationId = site,
            MatchingStrategy = MatchingStrategy.ByName, Group = group, Active = active
        };
        db.Datasets.Add(dataset);
        return dataset;
    }
}
