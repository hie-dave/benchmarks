using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Tests.Helpers;
using Dave.Benchmarks.Web.Controllers;
using LpjGuess.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class DataControllerTests
{
    [Fact]
    public async Task Index_ReturnsGroupedDatasetsView()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        SeedGroupAndDataset(db);
        DataController controller = CreateController(db);

        IActionResult result = await controller.Index();

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.NotNull(view.Model);
    }

    [Fact]
    public async Task Timeseries_ReturnsGroupedDatasetsView()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        EvaluationSeed.AddVariableLayer(db, dataset);
        DataController controller = CreateController(db);

        IActionResult result = await controller.Timeseries();

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.NotNull(view.Model);
    }

    [Fact]
    public async Task GetDatasets_ReturnsDatasets()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<Dataset>> result = await controller.GetDatasets();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<Dataset> datasets = Assert.IsAssignableFrom<IEnumerable<Dataset>>(ok.Value);
        Assert.Contains(datasets, d => d.Id == dataset.Id);
    }

    [Fact]
    public async Task GetGroups_ReturnsGroups()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DatasetGroup group = SeedGroupAndDataset(db);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<DatasetGroup>> result = await controller.GetGroups();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<DatasetGroup> groups = Assert.IsAssignableFrom<IEnumerable<DatasetGroup>>(ok.Value);
        Assert.Contains(groups, g => g.Id == group.Id);
    }

    [Fact]
    public async Task GetGroup_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DataController controller = CreateController(db);

        ActionResult<DatasetGroup> result = await controller.GetGroup(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGroup_WhenFound_ReturnsGroup()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DatasetGroup group = SeedGroupAndDataset(db);
        DataController controller = CreateController(db);

        ActionResult<DatasetGroup> result = await controller.GetGroup(group.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        DatasetGroup returned = Assert.IsType<DatasetGroup>(ok.Value);
        Assert.Equal(group.Id, returned.Id);
    }

    [Fact]
    public async Task GetDatasetsInGroup_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<Dataset>> result = await controller.GetDatasetsInGroup(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDatasetsInGroup_WhenFound_ReturnsDatasets()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DatasetGroup group = SeedGroupAndDataset(db);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<Dataset>> result = await controller.GetDatasetsInGroup(group.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<Dataset> datasets = Assert.IsAssignableFrom<IEnumerable<Dataset>>(ok.Value);
        Assert.NotEmpty(datasets);
    }

    [Fact]
    public async Task GetDatasetMetadata_WhenDatasetMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DataController controller = CreateController(db);

        ActionResult<object> result = await controller.GetDatasetMetadata(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDatasetMetadata_WhenNotPrediction_ReturnsBadRequest()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        ObservationDataset observation = EvaluationSeed.CreateObservationDataset(db);
        DataController controller = CreateController(db);

        ActionResult<object> result = await controller.GetDatasetMetadata(observation.Id);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDatasetMetadata_WhenPrediction_ReturnsParsedMetadata()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset prediction = EvaluationSeed.CreatePredictionDataset(db);
        prediction.Metadata = """{"site":"AU-How","year":2025}""";
        db.SaveChanges();

        DataController controller = CreateController(db);
        ActionResult<object> result = await controller.GetDatasetMetadata(prediction.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        System.Text.Json.JsonElement metadata = Assert.IsType<System.Text.Json.JsonElement>(ok.Value);
        Assert.Equal("AU-How", metadata.GetProperty("site").GetString());
        Assert.Equal(2025, metadata.GetProperty("year").GetInt32());
    }

    [Fact]
    public async Task GetVariables_WhenNoVariables_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<Variable>> result = await controller.GetVariables(dataset.Id);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetVariables_WhenPresent_ReturnsVariables()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, _) = EvaluationSeed.AddVariableLayer(db, dataset);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<Variable>> result = await controller.GetVariables(dataset.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<Variable> variables = Assert.IsAssignableFrom<IEnumerable<Variable>>(ok.Value);
        Assert.Contains(variables, v => v.Id == variable.Id);
    }

    [Fact]
    public async Task GetLayers_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<VariableLayer>> result = await controller.GetLayers(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetLayers_WhenFound_ReturnsLayers()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<VariableLayer>> result = await controller.GetLayers(variable.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<VariableLayer> layers = Assert.IsAssignableFrom<IEnumerable<VariableLayer>>(ok.Value);
        Assert.Contains(layers, l => l.Id == layer.Id);
    }

    [Fact]
    public async Task GetData_AppliesLayerAndDateFilters()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, VariableLayer layerA) = EvaluationSeed.AddVariableLayer(db, dataset);
        VariableLayer layerB = new() { Name = "b", Description = "b", VariableId = variable.Id };
        db.VariableLayers.Add(layerB);
        db.SaveChanges();

        DateTime t1 = new(2025, 1, 1);
        DateTime t2 = new(2025, 1, 2);
        db.GridcellData.Add(new GridcellDatum { VariableId = variable.Id, LayerId = layerA.Id, Timestamp = t1, Latitude = -33, Longitude = 151, Value = 1.0 });
        db.GridcellData.Add(new GridcellDatum { VariableId = variable.Id, LayerId = layerB.Id, Timestamp = t2, Latitude = -33, Longitude = 151, Value = 2.0 });
        db.SaveChanges();

        DataController controller = CreateController(db);
        ActionResult<IEnumerable<Datum>> result = await controller.GetData(dataset.Id, variable.Id, layerA.Id, t1, t1);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<Datum> data = Assert.IsAssignableFrom<IEnumerable<Datum>>(ok.Value);
        Datum only = Assert.Single(data);
        Assert.Equal(layerA.Id, only.LayerId);
    }

    [Fact]
    public async Task GetDataset_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DataController controller = CreateController(db);

        ActionResult<Dataset> result = await controller.GetDataset(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDataset_WhenFound_ReturnsDataset()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        DataController controller = CreateController(db);

        ActionResult<Dataset> result = await controller.GetDataset(dataset.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Dataset returned = Assert.IsType<PredictionDataset>(ok.Value);
        Assert.Equal(dataset.Id, returned.Id);
    }

    [Fact]
    public async Task GetVariableData_WhenVariableMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<object>> result = await controller.GetVariableData(dataset.Id, 999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetVariableData_WhenNoData_ReturnsEmptyArray()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, _) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Gridcell);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<object>> result = await controller.GetVariableData(dataset.Id, variable.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<object> data = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Empty(data);
    }

    [Fact]
    public async Task GetVariableData_WhenGridcellDataExists_ReturnsProjectedRows()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Gridcell);
        EvaluationSeed.AddGridcellDatum(db, variable, layer, new DateTime(2025, 1, 1), -33, 151, 1.1);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<object>> result = await controller.GetVariableData(dataset.Id, variable.Id, layer.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<object> data = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetVariableData_WhenStandDataExists_ReturnsProjectedRows()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Stand);
        db.StandData.Add(new StandDatum
        {
            VariableId = variable.Id,
            LayerId = layer.Id,
            Timestamp = new DateTime(2025, 1, 1),
            Latitude = -33,
            Longitude = 151,
            StandId = 2,
            Value = 1.2
        });
        db.SaveChanges();

        DataController controller = CreateController(db);
        ActionResult<IEnumerable<object>> result = await controller.GetVariableData(dataset.Id, variable.Id, layer.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<object> data = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetVariableData_WhenPatchDataExists_ReturnsProjectedRows()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Patch);
        db.PatchData.Add(new PatchDatum
        {
            VariableId = variable.Id,
            LayerId = layer.Id,
            Timestamp = new DateTime(2025, 1, 1),
            Latitude = -33,
            Longitude = 151,
            StandId = 2,
            PatchId = 3,
            Value = 1.2
        });
        db.SaveChanges();

        DataController controller = CreateController(db);
        ActionResult<IEnumerable<object>> result = await controller.GetVariableData(dataset.Id, variable.Id, layer.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<object> data = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetVariableData_WhenIndividualDataExists_ReturnsProjectedRows()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset, AggregationLevel.Individual);
        Individual individual = EvaluationSeed.EnsureIndividual(db, dataset, individualNumber: 4, pftName: "Tree");
        EvaluationSeed.AddIndividualDatum(db, variable, layer, individual, new DateTime(2025, 1, 1), -33, 151, 0.9);

        DataController controller = CreateController(db);
        ActionResult<IEnumerable<object>> result = await controller.GetVariableData(dataset.Id, variable.Id, layer.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<object> data = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetVariableData_WhenAggregationLevelUnknown_ReturnsBadRequest()
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

        DataController controller = CreateController(db);
        ActionResult<IEnumerable<object>> result = await controller.GetVariableData(dataset.Id, variable.Id);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetVariableLayers_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<VariableLayer>> result = await controller.GetVariableLayers(dataset.Id, 999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetVariableLayers_WhenFound_ReturnsLayers()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        (Variable variable, VariableLayer layer) = EvaluationSeed.AddVariableLayer(db, dataset);
        DataController controller = CreateController(db);

        ActionResult<IEnumerable<VariableLayer>> result = await controller.GetVariableLayers(dataset.Id, variable.Id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IEnumerable<VariableLayer> layers = Assert.IsAssignableFrom<IEnumerable<VariableLayer>>(ok.Value);
        Assert.Contains(layers, l => l.Id == layer.Id);
    }

    [Fact]
    public async Task DeleteDataset_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DataController controller = CreateController(db);

        ActionResult result = await controller.DeleteDataset(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteDataset_WhenExists_ReturnsSuccess()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        DataController controller = CreateController(db);

        ActionResult result = await controller.DeleteDataset(dataset.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.Datasets.Any(d => d.Id == dataset.Id));
    }

    [Fact]
    public async Task DeleteGroup_WhenMissing_ReturnsNotFound()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DataController controller = CreateController(db);

        ActionResult result = await controller.DeleteGroup(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteGroup_WhenGroupExists_RemovesGroupAndDatasets()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();

        DatasetGroup group = SeedGroupAndDataset(db);
        int datasetId = group.Datasets.Single().Id;

        DataController controller = CreateController(db);
        ActionResult result = await controller.DeleteGroup(group.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.DatasetGroups.Any(g => g.Id == group.Id));
        Assert.False(db.Datasets.Any(d => d.Id == datasetId));
    }

    private static DataController CreateController(BenchmarksDbContext db)
    {
        return new DataController(db, Mock.Of<ILogger<DataController>>());
    }

    private static DatasetGroup SeedGroupAndDataset(BenchmarksDbContext db)
    {
        DatasetGroup group = new()
        {
            Name = "group-a",
            Description = "desc",
            CreatedAt = DateTime.UtcNow,
            Metadata = "{}"
        };
        db.DatasetGroups.Add(group);
        db.SaveChanges();

        PredictionDataset dataset = EvaluationSeed.CreatePredictionDataset(db);
        dataset.GroupId = group.Id;
        db.SaveChanges();
        db.Entry(group).Collection(g => g.Datasets).Load();

        return group;
    }

}
