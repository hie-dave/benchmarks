using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Core.Data;

/// <summary>
/// Entity Framework Core context for the benchmarks database
/// </summary>
public class BenchmarksContext : DbContext
{
    public BenchmarksContext(DbContextOptions<BenchmarksContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entity relationships and constraints here
    }
}
