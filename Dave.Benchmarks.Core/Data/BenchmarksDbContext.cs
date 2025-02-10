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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TPT inheritance for data points
        modelBuilder.Entity<Datum>()
            .UseTptMappingStrategy();

        // Configure dataset discriminator
        modelBuilder.Entity<Dataset>()
            .HasDiscriminator<string>("DatasetType")
            .HasValue<PredictionDataset>("Prediction")
            .HasValue<ObservationDataset>("Observation")
            .IsComplete(true);  // Ensures only these two types are allowed

        // Configure relationships
        modelBuilder.Entity<Variable>()
            .HasOne(v => v.Dataset)
            .WithMany(d => d.Variables)
            .HasForeignKey(v => v.DatasetId);

        modelBuilder.Entity<Variable>()
            .HasMany(v => v.Layers)
            .WithOne(l => l.Variable)
            .HasForeignKey(l => l.VariableId);

        modelBuilder.Entity<VariableLayer>()
            .HasOne(l => l.Variable)
            .WithMany(v => v.Layers)
            .HasForeignKey(l => l.VariableId);

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
            .HasOne(i => i.Pft)
            .WithMany(p => p.Individuals)
            .HasForeignKey(i => i.PftId);

        modelBuilder.Entity<Individual>()
            .HasOne(i => i.Dataset)
            .WithMany()
            .HasForeignKey(i => i.DatasetId);

        // Ensure individual numbers are unique within a dataset
        modelBuilder.Entity<Individual>()
            .HasIndex(i => new { i.DatasetId, i.Number })
            .IsUnique();

        modelBuilder.Entity<IndividualDatum>()
            .HasOne(d => d.Individual)
            .WithMany(i => i.Data)
            .HasForeignKey(d => d.IndividualId);

        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.VariableId);
        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.LayerId);
        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.IndividualId);
        modelBuilder.Entity<IndividualDatum>()
            .HasIndex(d => d.Timestamp);
    }
}
