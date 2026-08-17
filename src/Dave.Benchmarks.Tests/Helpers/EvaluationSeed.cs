using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Models.Entities;
using LpjGuess.Core.Models.Entities;

namespace Dave.Benchmarks.Tests.Helpers;

public static class EvaluationSeed
{
    public static PredictionDataset CreatePredictionDataset(
        BenchmarksDbContext db,
        string simulationId = "sim-a",
        string baselineChannel = "main",
        string name = "candidate")
    {
        PredictionDataset dataset = new()
        {
            Name = name,
            Description = name,
            CreatedAt = DateTime.UtcNow,
            SpatialResolution = "site",
            TemporalResolution = "daily",
            SimulationId = simulationId,
            BaselineChannel = baselineChannel,
            ModelVersion = "abc123",
            ClimateDataset = "climate",
            Metadata = "{}",
            Patches = []
        };

        db.Datasets.Add(dataset);
        db.SaveChanges();
        return dataset;
    }

    public static ObservationDataset CreateObservationDataset(
        BenchmarksDbContext db,
        MatchingStrategy strategy = MatchingStrategy.ExactMatch,
        int? maxDistance = null,
        bool active = true,
        string simulationId = "",
        bool? complete = null)
    {
        DatasetGroup group = new()
        {
            Name = $"observations-{Guid.NewGuid():N}",
            Description = "test observations",
            CreatedAt = DateTime.UtcNow,
            IsComplete = complete ?? active,
            IsActive = active,
            Kind = strategy == MatchingStrategy.ByName
                ? DatasetGroupKind.ObservationSite
                : DatasetGroupKind.ObservationGridded,
            Source = "source",
            Version = "v1",
            Metadata = "{}"
        };
        if (active) group.ActiveCollectionKey = $"{group.Source}\n{group.Name}";
        db.DatasetGroups.Add(group);
        ObservationDataset dataset = new()
        {
            Name = "obs",
            Description = "obs",
            CreatedAt = DateTime.UtcNow,
            SpatialResolution = "grid",
            TemporalResolution = "daily",
            SimulationId = simulationId,
            Source = "source",
            Version = "v1",
            Metadata = "{}",
            MatchingStrategy = strategy,
            MaxDistance = maxDistance,
            Active = active,
            Group = group
        };

        db.Datasets.Add(dataset);
        db.SaveChanges();
        return dataset;
    }

    public static (Variable Variable, VariableLayer Layer) AddVariableLayer(
        BenchmarksDbContext db,
        Dataset dataset,
        AggregationLevel level = AggregationLevel.Gridcell,
        string variableName = "lai",
        string units = "m2m2",
        string layerName = "total")
    {
        Variable variable = new()
        {
            Name = variableName,
            Description = variableName,
            Level = level,
            Units = units,
            DatasetId = dataset.Id
        };

        db.Variables.Add(variable);
        db.SaveChanges();

        VariableLayer layer = new()
        {
            Name = layerName,
            Description = layerName,
            VariableId = variable.Id
        };
        db.VariableLayers.Add(layer);
        db.SaveChanges();

        return (variable, layer);
    }

    public static void AddGridcellDatum(
        BenchmarksDbContext db,
        Variable variable,
        VariableLayer layer,
        DateTime timestamp,
        double latitude,
        double longitude,
        double value)
    {
        db.GridcellData.Add(new GridcellDatum
        {
            Timestamp = timestamp,
            Latitude = latitude,
            Longitude = longitude,
            Value = value,
            VariableId = variable.Id,
            LayerId = layer.Id
        });
        db.SaveChanges();
    }

    public static Individual EnsureIndividual(
        BenchmarksDbContext db,
        Dataset dataset,
        int individualNumber = 1,
        string pftName = "tree")
    {
        Pft? pft = db.Pfts.FirstOrDefault(p => p.Name == pftName);
        if (pft == null)
        {
            pft = new Pft { Name = pftName };
            db.Pfts.Add(pft);
            db.SaveChanges();
        }

        Individual individual = new()
        {
            DatasetId = dataset.Id,
            Number = individualNumber,
            PftId = pft.Id
        };
        db.Individuals.Add(individual);
        db.SaveChanges();
        return individual;
    }

    public static void AddIndividualDatum(
        BenchmarksDbContext db,
        Variable variable,
        VariableLayer layer,
        Individual individual,
        DateTime timestamp,
        double latitude,
        double longitude,
        double value,
        int standId = 1,
        int patchId = 1)
    {
        db.IndividualData.Add(new IndividualDatum
        {
            Timestamp = timestamp,
            Latitude = latitude,
            Longitude = longitude,
            Value = value,
            StandId = standId,
            PatchId = patchId,
            IndividualId = individual.Id,
            VariableId = variable.Id,
            LayerId = layer.Id
        });
        db.SaveChanges();
    }

    public static EvaluationRun CreateRun(
        BenchmarksDbContext db,
        PredictionDataset candidate,
        int? baselineDatasetId = null)
    {
        BenchmarkSubmission submission = new()
        {
            GitLabProjectId = "1",
            MergeRequestId = "123",
            PipelineId = Guid.NewGuid().ToString(),
            SourceBranch = "feature",
            TargetBranch = "main",
            CommitSha = "abcdef",
            BenchmarkName = "test",
            Status = BenchmarkSubmissionStatus.Complete,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        db.BenchmarkSubmissions.Add(submission);
        db.SaveChanges();
        EvaluationRun run = new()
        {
            BenchmarkSubmissionId = submission.Id,
            Status = EvaluationRunStatus.Pending,
            StartedAt = DateTime.UtcNow
        };
        run.Datasets.Add(new EvaluationRunDataset
        {
            CandidateDatasetId = candidate.Id,
            BaselineDatasetId = baselineDatasetId,
            Status = EvaluationRunStatus.Pending
        });
        db.EvaluationRuns.Add(run);
        db.SaveChanges();
        return run;
    }
}
