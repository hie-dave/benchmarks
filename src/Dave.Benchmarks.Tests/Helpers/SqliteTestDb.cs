using Dave.Benchmarks.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dave.Benchmarks.Tests.Helpers;

public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection connection;

    private SqliteTestDb(SqliteConnection connection)
    {
        this.connection = connection;
    }

    public static SqliteTestDb Create()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        return new SqliteTestDb(connection);
    }

    public BenchmarksDbContext CreateContext()
    {
        DbContextOptions<BenchmarksDbContext> options = new DbContextOptionsBuilder<BenchmarksDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        BenchmarksDbContext context = new(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        connection.Dispose();
    }
}
