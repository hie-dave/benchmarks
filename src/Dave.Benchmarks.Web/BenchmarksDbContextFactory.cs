using Dave.Benchmarks.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dave.Benchmarks.Web;

/// <summary>
/// Creates the model for EF design-time commands without requiring a running
/// MariaDB server. Runtime configuration and server-version detection remain in
/// Program.cs.
/// </summary>
public sealed class BenchmarksDbContextFactory : IDesignTimeDbContextFactory<BenchmarksDbContext>
{
    public BenchmarksDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<BenchmarksDbContext> options = new();
        options.UseMySql(
            "Server=localhost;Database=dave_benchmarks;User=dave",
            MariaDbServerVersion.LatestSupportedServerVersion,
            mysql => mysql.MigrationsAssembly("Dave.Benchmarks.Web"));

        return new BenchmarksDbContext(options.Options);
    }
}
