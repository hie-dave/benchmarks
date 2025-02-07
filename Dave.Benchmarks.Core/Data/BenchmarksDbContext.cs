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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure dataset inheritance
        modelBuilder.Entity<Dataset>()
            .HasDiscriminator<string>("DatasetType")
            .HasValue<PredictionDataset>("Prediction")
            .HasValue<ObservationDataset>("Observation")
            .IsComplete(true);  // Ensures only these two types are allowed

        // Configure relationships
        modelBuilder.Entity<Variable>()
            .HasMany(v => v.Layers)
            .WithOne(l => l.Variable)
            .HasForeignKey(l => l.VariableId);

        // Add indexes for common query patterns
        modelBuilder.Entity<GridcellDatum>()
            .HasIndex(d => new { d.VariableId, d.LayerId, d.Timestamp });
        
        modelBuilder.Entity<StandDatum>()
            .HasIndex(d => new { d.VariableId, d.LayerId, d.StandId, d.Timestamp });
        
        modelBuilder.Entity<PatchDatum>()
            .HasIndex(d => new { d.VariableId, d.LayerId, d.StandId, d.PatchId, d.Timestamp });
    }
}
