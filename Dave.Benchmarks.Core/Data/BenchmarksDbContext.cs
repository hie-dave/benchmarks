using Microsoft.EntityFrameworkCore;
using Dave.Benchmarks.Core.Models;

namespace Dave.Benchmarks.Core.Data;

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
    public DbSet<DataPoint> DataPoints { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure TPT inheritance
        modelBuilder.Entity<Dataset>()
            .UseTptMappingStrategy();

        // Configure indexes for better query performance
        modelBuilder.Entity<DataPoint>()
            .HasIndex(d => new { d.DatasetId, d.VariableId, d.Longitude, d.Latitude });
        
        modelBuilder.Entity<DataPoint>()
            .HasIndex(d => new { d.DatasetId, d.VariableId, d.Timestamp });

        // Configure relationships
        modelBuilder.Entity<Dataset>()
            .HasMany(d => d.Variables)
            .WithOne(v => v.Dataset)
            .HasForeignKey(v => v.DatasetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dataset>()
            .HasMany(d => d.DataPoints)
            .WithOne(p => p.Dataset)
            .HasForeignKey(p => p.DatasetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Variable>()
            .HasMany(v => v.DataPoints)
            .WithOne(p => p.Variable)
            .HasForeignKey(p => p.VariableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
