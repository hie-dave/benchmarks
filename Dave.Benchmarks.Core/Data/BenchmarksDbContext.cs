using Microsoft.EntityFrameworkCore;
using Dave.Benchmarks.Core.Models.Entities;

namespace Dave.Benchmarks.Core.Data;

/// <summary>
/// Entity Framework Core context for the benchmarks database.
/// </summary>
public class BenchmarksDbContext : DbContext
{
    public BenchmarksDbContext(DbContextOptions<BenchmarksDbContext> options)
        : base(options)
    {
    }

    public DbSet<Dataset> Datasets { get; set; } = null!;
    public DbSet<ModelPredictionDataset> ModelPredictions { get; set; } = null!;
    public DbSet<ObservationDataset> Observations { get; set; } = null!;
    public DbSet<Variable> Variables { get; set; } = null!;
    public DbSet<Datum> MeasurementPoints { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TPT inheritance for datasets
        modelBuilder.Entity<Dataset>()
            .UseTptMappingStrategy();

        // Configure indexes for better query performance
        modelBuilder.Entity<Datum>()
            .HasIndex(d => new { d.DatasetId, d.VariableId, d.Longitude, d.Latitude });
        
        modelBuilder.Entity<Datum>()
            .HasIndex(d => new { d.DatasetId, d.VariableId, d.Timestamp });

        // Configure relationships
        modelBuilder.Entity<Dataset>()
            .HasMany(d => d.Variables)
            .WithOne(v => v.Dataset)
            .HasForeignKey(v => v.DatasetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dataset>()
            .HasMany(d => d.Data)
            .WithOne(p => p.Dataset)
            .HasForeignKey(p => p.DatasetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Variable>()
            .HasMany(v => v.Data)
            .WithOne(p => p.Variable)
            .HasForeignKey(p => p.VariableId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure table names
        modelBuilder.Entity<Datum>()
            .ToTable("Data");
        
        modelBuilder.Entity<Variable>()
            .ToTable("Variables");

        modelBuilder.Entity<ModelPredictionDataset>()
            .ToTable("Predictions");

        modelBuilder.Entity<ObservationDataset>()
            .ToTable("Observations");
    }
}
