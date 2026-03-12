using LpjGuess.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Services.Metrics;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Core.Data;

/// <summary>
/// Database context for the benchmarking system.
/// </summary>
public class BenchmarksDbContext : DbContext
{
    public BenchmarksDbContext(DbContextOptions<BenchmarksDbContext> options)
        : base(options)
    {
    }

    public DbSet<Dataset> Datasets { get; set; } = null!;
    public DbSet<Variable> Variables { get; set; } = null!;
    public DbSet<VariableLayer> VariableLayers { get; set; } = null!;
    public DbSet<GridcellDatum> GridcellData { get; set; } = null!;
    public DbSet<StandDatum> StandData { get; set; } = null!;
    public DbSet<PatchDatum> PatchData { get; set; } = null!;
    public DbSet<Pft> Pfts { get; set; } = null!;
    public DbSet<Individual> Individuals { get; set; } = null!;
    public DbSet<IndividualDatum> IndividualData { get; set; } = null!;
    public DbSet<DatasetGroup> DatasetGroups { get; set; } = null!;
    public DbSet<PredictionBaselineRegistryEntry> PredictionBaselineRegistryEntries { get; set; } = null!;
    public DbSet<EvaluationRun> EvaluationRuns { get; set; } = null!;
    public DbSet<EvaluationResult> EvaluationResults { get; set; } = null!;
    public DbSet<EvaluationMetric> EvaluationMetrics { get; set; } = null!;

    public override int SaveChanges()
    {
        ValidatePendingEvaluationEntities();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidatePendingEvaluationEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidatePendingEvaluationEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidatePendingEvaluationEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidatePendingEvaluationEntities()
    {
        foreach (var entry in ChangeTracker.Entries<EvaluationMetric>())
        {
            if (entry.State is not EntityState.Added and not EntityState.Modified)
                continue;

            string metricType = entry.Entity.MetricType?.Trim() ?? string.Empty;
            entry.Entity.MetricType = metricType;
            if (!BuiltInMetrics.IsKnownType(metricType))
            {
                throw new InvalidOperationException(
                    $"Unknown metric key '{metricType}'. Register metric implementations before persisting results.");
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TPT inheritance for data points
        modelBuilder.Entity<Datum>()
            .UseTptMappingStrategy();

        // Configure dataset discriminator
        modelBuilder.Entity<Dataset>()
            .HasDiscriminator<string>("DatasetType")
            .HasValue<ObservationDataset>("Observation")
            .HasValue<PredictionDataset>("Prediction")
            .IsComplete(true);  // Ensures only these two types are allowed

        // For observation datasets only:
        // - Nearest strategy requires MaxDistance > 0
        // - Non-nearest strategies require MaxDistance to be null
        modelBuilder.Entity<Dataset>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Datasets_Observation_MatchingStrategy_MaxDistance",
                "(DatasetType <> 'Observation') OR " +
                "((MatchingStrategy = 1 AND MaxDistance IS NOT NULL AND MaxDistance > 0) OR " +
                "(MatchingStrategy <> 1 AND MaxDistance IS NULL))"));

        modelBuilder.Entity<Dataset>()
            .Property(d => d.SimulationId)
            .HasMaxLength(128)
            .IsRequired();

        modelBuilder.Entity<Dataset>()
            .HasIndex(d => d.SimulationId);

        modelBuilder.Entity<PredictionDataset>()
            .Property(d => d.BaselineChannel)
            .HasMaxLength(128)
            .IsRequired();

        modelBuilder.Entity<PredictionDataset>()
            .HasIndex(d => new { d.SimulationId, d.BaselineChannel });

        modelBuilder.Entity<ObservationDataset>()
            .Property(d => d.MatchingStrategy)
            .IsRequired();

        modelBuilder.Entity<ObservationDataset>()
            .Property(d => d.Active)
            .HasDefaultValue(false);

        modelBuilder.Entity<ObservationDataset>()
            .HasIndex(d => d.Active);

        // Configure relationships with cascade delete
        modelBuilder.Entity<Dataset>()
            .HasOne(d => d.Group)
            .WithMany(g => g.Datasets)
            .HasForeignKey(d => d.GroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Variable>()
            .HasOne(v => v.Dataset)
            .WithMany(d => d.Variables)
            .HasForeignKey(v => v.DatasetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VariableLayer>()
            .HasOne(l => l.Variable)
            .WithMany(v => v.Layers)
            .HasForeignKey(l => l.VariableId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enables composite FKs to guarantee layer-variable consistency
        // in evaluation result mappings.
        modelBuilder.Entity<VariableLayer>()
            .HasAlternateKey(l => new { l.Id, l.VariableId });

        // Configure data point relationships with cascade delete
        modelBuilder.Entity<Datum>()
            .HasOne(d => d.Variable)
            .WithMany()
            .HasForeignKey(d => d.VariableId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Datum>()
            .HasOne(d => d.Layer)
            .WithMany()
            .HasForeignKey(d => d.LayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Individual-level data with cascade delete
        modelBuilder.Entity<Individual>()
            .HasOne(i => i.Dataset)
            .WithMany()
            .HasForeignKey(i => i.DatasetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pft>()
            .HasMany(p => p.Individuals)
            .WithOne(i => i.Pft)
            .HasForeignKey(i => i.PftId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete PFTs

        modelBuilder.Entity<IndividualDatum>()
            .HasOne(d => d.Individual)
            .WithMany(i => i.Data)
            .HasForeignKey(d => d.IndividualId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes for common query patterns
        modelBuilder.Entity<GridcellDatum>()
            .HasIndex(d => d.VariableId);
        modelBuilder.Entity<GridcellDatum>()
            .HasIndex(d => d.LayerId);
        modelBuilder.Entity<GridcellDatum>()
            .HasIndex(d => d.Timestamp);
        
        modelBuilder.Entity<StandDatum>()
            .HasIndex(d => d.VariableId);
        modelBuilder.Entity<StandDatum>()
            .HasIndex(d => d.LayerId);
        modelBuilder.Entity<StandDatum>()
            .HasIndex(d => d.Timestamp);
        modelBuilder.Entity<StandDatum>()
            .HasIndex(d => d.StandId);
        
        modelBuilder.Entity<PatchDatum>()
            .HasIndex(d => d.VariableId);
        modelBuilder.Entity<PatchDatum>()
            .HasIndex(d => d.LayerId);
        modelBuilder.Entity<PatchDatum>()
            .HasIndex(d => d.Timestamp);
        modelBuilder.Entity<PatchDatum>()
            .HasIndex(d => d.PatchId);

        // Configure Individual-level data
        modelBuilder.Entity<Pft>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Individual>()
            .HasIndex(i => new { i.DatasetId, i.Number })
            .IsUnique();

        // Add indexes for individual data
        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.VariableId);
        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.LayerId);
        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.IndividualId);
        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.Timestamp);

        modelBuilder.Entity<PredictionBaselineRegistryEntry>(entity =>
        {
            entity.Property(e => e.SimulationId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.BaselineChannel)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasOne(e => e.PredictionDataset)
                .WithMany()
                .HasForeignKey(e => e.PredictionDatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.AcceptedBy)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.AcceptedReason)
                .HasMaxLength(1024);

            entity.Property(e => e.AcceptedFromPipelineId)
                .HasMaxLength(128);

            entity.HasIndex(e => new { e.SimulationId, e.BaselineChannel })
                .HasDatabaseName("IX_PredictionBaselineRegistryEntries_SimulationId_BaselineChannel");

            entity.HasIndex(e => e.PredictionDatasetId);
            entity.HasIndex(e => e.AcceptedAt);
        });

        modelBuilder.Entity<EvaluationRun>(entity =>
        {
            entity.Property(e => e.SimulationId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.BaselineChannel)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.MergeRequestId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.SourceBranch)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.TargetBranch)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.CommitSha)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasOne(e => e.CandidateDataset)
                .WithMany()
                .HasForeignKey(e => e.CandidateDatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BaselineDataset)
                .WithMany()
                .HasForeignKey(e => e.BaselineDatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SimulationId, e.BaselineChannel });
            entity.HasIndex(e => e.CandidateDatasetId);
            entity.HasIndex(e => e.BaselineDatasetId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<EvaluationResult>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_EvaluationResults_BaselineVariableLayerPair",
                "((BaselineVariableId IS NULL AND BaselineLayerId IS NULL) OR " +
                "(BaselineVariableId IS NOT NULL AND BaselineLayerId IS NOT NULL))"));

            entity.HasOne(e => e.EvaluationRun)
                .WithMany(r => r.Results)
                .HasForeignKey(e => e.EvaluationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CandidateVariable)
                .WithMany()
                .HasForeignKey(e => e.CandidateVariableId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CandidateLayer)
                .WithMany()
                .HasPrincipalKey(l => new { l.Id, l.VariableId })
                .HasForeignKey(e => new { e.CandidateLayerId, e.CandidateVariableId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BaselineVariable)
                .WithMany()
                .HasForeignKey(e => e.BaselineVariableId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BaselineLayer)
                .WithMany()
                .HasPrincipalKey(l => new { l.Id, l.VariableId })
                .HasForeignKey(e => new { e.BaselineLayerId, e.BaselineVariableId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ObservationVariable)
                .WithMany()
                .HasForeignKey(e => e.ObservationVariableId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ObservationLayer)
                .WithMany()
                .HasPrincipalKey(l => new { l.Id, l.VariableId })
                .HasForeignKey(e => new { e.ObservationLayerId, e.ObservationVariableId })
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.EvaluationRunId);
            entity.HasIndex(e => new
            {
                e.EvaluationRunId,
                e.CandidateVariableId,
                e.CandidateLayerId,
                e.ObservationVariableId,
                e.ObservationLayerId
            })
                .IsUnique();
            entity.HasIndex(e => e.CandidateVariableId);
            entity.HasIndex(e => e.CandidateLayerId);
            entity.HasIndex(e => e.BaselineVariableId);
            entity.HasIndex(e => e.BaselineLayerId);
            entity.HasIndex(e => e.ObservationVariableId);
            entity.HasIndex(e => e.ObservationLayerId);
        });

        modelBuilder.Entity<EvaluationMetric>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_EvaluationMetrics_Value_IsFinite",
                "(Value = Value) AND " +
                "(Value <= 1.7976931348623157E308) AND " +
                "(Value >= -1.7976931348623157E308)"));

            entity.Property(e => e.MetricType)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasOne(e => e.EvaluationResult)
                .WithMany(r => r.Metrics)
                .HasForeignKey(e => e.EvaluationResultId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.EvaluationResultId);
            entity.HasIndex(e => new { e.EvaluationResultId, e.MetricType })
                .IsUnique();
        });
    }
}
