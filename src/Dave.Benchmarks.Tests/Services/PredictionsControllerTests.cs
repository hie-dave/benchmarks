using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Tests.Helpers;
using Dave.Benchmarks.Web.Controllers;
using LpjGuess.Core.Models.Entities;
using LpjGuess.Core.Models.Importer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class PredictionsControllerTests
{
    [Fact]
    public async Task CreateDatasetGroup_CreatesGroupAndReturnsId()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateDatasetGroup(new CreateDatasetGroupRequest
        {
            Name = "group-a",
            Description = "desc",
            Metadata = "{}"
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int groupId = Assert.IsType<int>(ok.Value);
        Assert.True(db.DatasetGroups.Any(g => g.Id == groupId && g.Name == "group-a"));
    }

    [Fact]
    public async Task CompleteDatasetGroup_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionsController controller = CreateController(db);

        ActionResult result = await controller.CompleteDatasetGroup(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CompleteDatasetGroup_WhenFound_MarksComplete()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DatasetGroup group = new()
        {
            Name = "group-a",
            Description = "desc",
            CreatedAt = DateTime.UtcNow,
            Metadata = "{}"
        };
        db.DatasetGroups.Add(group);
        db.SaveChanges();

        PredictionsController controller = CreateController(db);
        ActionResult result = await controller.CompleteDatasetGroup(group.Id);

        Assert.IsType<OkResult>(result);
        Assert.True(db.DatasetGroups.Single(g => g.Id == group.Id).IsComplete);
    }

    [Fact]
    public async Task CreateDataset_WhenGroupMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateDataset(new CreateDatasetRequest
        {
            Name = "d1",
            Description = "desc",
            ModelVersion = "v1",
            ClimateDataset = "climate",
            TemporalResolution = "daily",
            SimulationId = "sim",
            BaselineChannel = "main",
            CompressedCodePatches = [],
            GroupId = 999
        });

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

        PredictionsController controller = CreateController(db);
        ActionResult<int> result = await controller.CreateDataset(new CreateDatasetRequest
        {
            Name = "d1",
            Description = "desc",
            ModelVersion = "v1",
            ClimateDataset = "climate",
            TemporalResolution = "daily",
            SimulationId = "sim",
            BaselineChannel = "main",
            CompressedCodePatches = [],
            GroupId = group.Id
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDataset_CreatesPredictionDatasetAndReturnsId()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateDataset(new CreateDatasetRequest
        {
            Name = "d1",
            Description = "desc",
            ModelVersion = "v1",
            ClimateDataset = "climate",
            TemporalResolution = "daily",
            SimulationId = "sim",
            BaselineChannel = "main",
            CompressedCodePatches = [1, 2, 3],
            Metadata = """{"x":1}"""
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int id = Assert.IsType<int>(ok.Value);
        PredictionDataset created = db.Datasets.OfType<PredictionDataset>().Single(d => d.Id == id);
        Assert.Equal("sim", created.SimulationId);
        Assert.Equal("main", created.BaselineChannel);
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
            Metadata = "{}"
        };
        db.DatasetGroups.Add(group);
        db.SaveChanges();

        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateDataset(new CreateDatasetRequest
        {
            Name = "d-grouped",
            Description = "desc",
            ModelVersion = "v1",
            ClimateDataset = "climate",
            TemporalResolution = "daily",
            SimulationId = "sim",
            BaselineChannel = "main",
            CompressedCodePatches = [],
            GroupId = group.Id
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int datasetId = Assert.IsType<int>(ok.Value);
        PredictionDataset created = db.Datasets.OfType<PredictionDataset>().Single(d => d.Id == datasetId);
        Assert.Equal(group.Id, created.GroupId);
    }

    [Fact]
    public async Task AddQuantity_WhenDatasetMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionsController controller = CreateController(db);

        ActionResult result = await controller.AddQuantity(999, BuildGridcellQuantity());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddQuantity_WhenIndividualWithoutPfts_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "lai",
            "lai",
            [new Layer("total", new Unit("m2m2"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0, 1, 1, 1)])],
            AggregationLevel.Individual,
            TemporalResolution.Daily);

        ActionResult result = await controller.AddQuantity(dataset.Id, quantity);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddQuantity_WhenNonIndividualHasPfts_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "lai",
            "lai",
            [new Layer("total", new Unit("m2m2"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)])],
            AggregationLevel.Gridcell,
            TemporalResolution.Daily,
            new Dictionary<int, string> { [1] = "Tree" });

        ActionResult result = await controller.AddQuantity(dataset.Id, quantity);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddQuantity_WhenNoLayers_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "lai",
            "lai",
            [],
            AggregationLevel.Gridcell,
            TemporalResolution.Daily);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AddQuantity(dataset.Id, quantity));
    }

    [Fact]
    public async Task AddQuantity_WhenLayerUnitsDiffer_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "lai",
            "lai",
            [
                new Layer("a", new Unit("m2m2"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)]),
                new Layer("b", new Unit("kg"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 2.0)])
            ],
            AggregationLevel.Gridcell,
            TemporalResolution.Daily);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AddQuantity(dataset.Id, quantity));
    }

    [Fact]
    public async Task AddQuantity_WhenVariableAlreadyExists_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Gridcell, variableName: "lai", units: "m2m2");

        PredictionsController controller = CreateController(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AddQuantity(dataset.Id, BuildGridcellQuantity()));
    }

    [Fact]
    public async Task AddQuantity_Gridcell_SavesVariableLayerAndData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        ActionResult result = await controller.AddQuantity(dataset.Id, BuildGridcellQuantity());

        Assert.IsType<OkResult>(result);
        Variable variable = db.Variables.Single(v => v.DatasetId == dataset.Id && v.Name == "lai");
        VariableLayer layer = db.VariableLayers.Single(l => l.VariableId == variable.Id);
        Assert.Equal("total", layer.Name);
        Assert.Equal(1, db.GridcellData.Count(d => d.VariableId == variable.Id));
    }

    [Fact]
    public async Task AddQuantity_Stand_SavesStandData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "biomass",
            "biomass",
            [new Layer("total", new Unit("kg"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 3.0, Stand: 5)])],
            AggregationLevel.Stand,
            TemporalResolution.Daily);

        ActionResult result = await controller.AddQuantity(dataset.Id, quantity);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.StandData.Count());
    }

    [Fact]
    public async Task AddQuantity_PatchWithoutPatchId_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "biomass",
            "biomass",
            [new Layer("total", new Unit("kg"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 3.0, Stand: 5)])],
            AggregationLevel.Patch,
            TemporalResolution.Daily);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AddQuantity(dataset.Id, quantity));
    }

    [Fact]
    public async Task AddQuantity_Patch_SavesPatchData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "biomass",
            "biomass",
            [new Layer("total", new Unit("kg"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 3.0, Stand: 5, Patch: 2)])],
            AggregationLevel.Patch,
            TemporalResolution.Daily);

        ActionResult result = await controller.AddQuantity(dataset.Id, quantity);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.PatchData.Count());
    }

    [Fact]
    public async Task AddQuantity_UnknownAggregationLevel_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "x",
            "x",
            [new Layer("total", new Unit("u"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)])],
            (AggregationLevel)999,
            TemporalResolution.Daily);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AddQuantity(dataset.Id, quantity));
    }

    [Fact]
    public async Task AddQuantity_IndividualWithConflictingExistingPft_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationSeed.EnsureIndividual(db, dataset, individualNumber: 7, pftName: "Tree");
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "fpc",
            "fpc",
            [new Layer("total", new Unit("frac"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 0.5, 1, 1, 7)])],
            AggregationLevel.Individual,
            TemporalResolution.Daily,
            new Dictionary<int, string> { [7] = "Grass" });

        ActionResult result = await controller.AddQuantity(dataset.Id, quantity);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddQuantity_IndividualWithMappings_SavesIndividualsAndData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        Quantity quantity = new(
            "fpc",
            "fpc",
            [new Layer("total", new Unit("frac"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 0.5, 1, 1, 7)])],
            AggregationLevel.Individual,
            TemporalResolution.Daily,
            new Dictionary<int, string> { [7] = "Tree" });

        ActionResult result = await controller.AddQuantity(dataset.Id, quantity);

        Assert.IsType<OkResult>(result);
        Assert.True(db.Individuals.Any(i => i.DatasetId == dataset.Id && i.Number == 7));
        Assert.Equal(1, db.IndividualData.Count());
    }

    [Fact]
    public async Task CreateVariable_WhenDatasetMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionsController controller = CreateController(db);

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
    public async Task CreateVariable_WhenIndividualWithoutPfts_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

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
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

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
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, _) = EvaluationSeed.AddVariableLayer(
            db,
            dataset,
            AggregationLevel.Gridcell,
            variableName: "lai",
            units: "m2m2");

        PredictionsController controller = CreateController(db);
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
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "fpc",
            Description = "fpc",
            Level = AggregationLevel.Individual,
            Units = "frac",
            IndividualPfts = new Dictionary<int, string> { [5] = "Tree" }
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        int variableId = Assert.IsType<int>(ok.Value);
        Assert.True(db.Variables.Any(v => v.Id == variableId));
        Assert.True(db.Individuals.Any(i => i.DatasetId == dataset.Id && i.Number == 5));
    }

    [Fact]
    public async Task CreateVariable_IndividualWithConflictingPft_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationSeed.EnsureIndividual(db, dataset, individualNumber: 5, pftName: "Tree");
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "fpc",
            Description = "fpc",
            Level = AggregationLevel.Individual,
            Units = "frac",
            IndividualPfts = new Dictionary<int, string> { [5] = "Grass" }
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateVariable_IndividualWithSameExistingPft_Succeeds()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationSeed.EnsureIndividual(db, dataset, individualNumber: 5, pftName: "Tree");
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateVariable(dataset.Id, new CreateVariableRequest
        {
            Name = "fpc",
            Description = "fpc",
            Level = AggregationLevel.Individual,
            Units = "frac",
            IndividualPfts = new Dictionary<int, string> { [5] = "Tree" }
        });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<int>(ok.Value) > 0);
    }

    [Fact]
    public async Task CreateLayer_WhenVariableMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateLayer(999, new CreateLayerRequest
        {
            Name = "layer-a",
            Description = "layer-a"
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_WhenVariableExists_ReturnsLayerId()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, _) = EvaluationSeed.AddVariableLayer(db, dataset, variableName: "lai", layerName: "old");
        PredictionsController controller = CreateController(db);

        ActionResult<int> result = await controller.CreateLayer(variable.Id, new CreateLayerRequest
        {
            Name = "new-layer",
            Description = "new-layer"
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
        PredictionsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(999, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)]
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AppendData_Gridcell_AppendsData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Gridcell);
        PredictionsController controller = CreateController(db);

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
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Stand);
        PredictionsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0, Stand: 3)]
        });

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.StandData.Count(d => d.LayerId == layer.Id));
    }

    [Fact]
    public async Task AppendData_StandWithoutStandId_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Stand);
        PredictionsController controller = CreateController(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)]
        }));
    }

    [Fact]
    public async Task AppendData_PatchWithoutPatchId_ThrowsInvalidOperationException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Patch);
        PredictionsController controller = CreateController(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0, Stand: 2)]
        }));
    }

    [Fact]
    public async Task AppendData_Patch_AppendsData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Patch);
        PredictionsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0, Stand: 2, Patch: 9)]
        });

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.PatchData.Count(d => d.LayerId == layer.Id));
    }

    [Fact]
    public async Task AppendData_UnknownAggregationLevel_ThrowsArgumentException()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        Variable variable = new()
        {
            Name = "x",
            Description = "x",
            Units = "u",
            DatasetId = dataset.Id,
            Level = (AggregationLevel)999
        };
        db.Variables.Add(variable);
        db.SaveChanges();
        VariableLayer layer = new() { Name = "total", Description = "total", VariableId = variable.Id };
        db.VariableLayers.Add(layer);
        db.SaveChanges();

        PredictionsController controller = CreateController(db);
        await Assert.ThrowsAsync<ArgumentException>(() => controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)]
        }));
    }

    [Fact]
    public async Task AppendData_Individual_AppendsData()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (_, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Individual);
        EvaluationSeed.EnsureIndividual(db, dataset, individualNumber: 11, pftName: "Tree");
        PredictionsController controller = CreateController(db);

        ActionResult result = await controller.AppendData(layer.Id, new AppendDataRequest
        {
            DataPoints = [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 0.4, 1, 1, 11)]
        });

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, db.IndividualData.Count(d => d.LayerId == layer.Id));
    }

    private static PredictionsController CreateController(BenchmarksDbContext db)
    {
        return new PredictionsController(db, Mock.Of<ILogger<PredictionsController>>());
    }

    private static Quantity BuildGridcellQuantity()
    {
        return new Quantity(
            "lai",
            "lai",
            [new Layer("total", new Unit("m2m2"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)])],
            AggregationLevel.Gridcell,
            TemporalResolution.Daily);
    }
}
