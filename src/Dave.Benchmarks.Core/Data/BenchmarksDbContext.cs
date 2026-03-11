using LpjGuess.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Entities;
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
    public DbSet<ObservationBaselineRegistryEntry> ObservationBaselineRegistryEntries { get; set; } = null!;
    public DbSet<EvaluationRun> EvaluationRuns { get; set; } = null!;
    public DbSet<EvaluationResult> EvaluationResults { get; set; } = null!;

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

            entity.HasIndex(e => new { e.SimulationId, e.BaselineChannel })
                .IsUnique();

            entity.HasIndex(e => e.PredictionDatasetId);
        });

        modelBuilder.Entity<ObservationBaselineRegistryEntry>(entity =>
        {
            entity.Property(e => e.SimulationId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.BaselineChannel)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasOne(e => e.ObservationDataset)
                .WithMany()
                .HasForeignKey(e => e.ObservationDatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SimulationId, e.BaselineChannel })
                .IsUnique();

            entity.HasIndex(e => e.ObservationDatasetId);
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

            entity.HasOne(e => e.CandidatePredictionDataset)
                .WithMany()
                .HasForeignKey(e => e.CandidatePredictionDatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BaselinePredictionDataset)
                .WithMany()
                .HasForeignKey(e => e.BaselinePredictionDatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ObservationBaselineDataset)
                .WithMany()
                .HasForeignKey(e => e.ObservationBaselineDatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SimulationId, e.BaselineChannel });
            entity.HasIndex(e => e.CandidatePredictionDatasetId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<EvaluationResult>(entity =>
        {
            entity.Property(e => e.VariableName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.LayerName)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasOne(e => e.EvaluationRun)
                .WithMany(r => r.Results)
                .HasForeignKey(e => e.EvaluationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.EvaluationRunId);
            entity.HasIndex(e => new { e.EvaluationRunId, e.VariableName, e.LayerName });
        });
    }
}
