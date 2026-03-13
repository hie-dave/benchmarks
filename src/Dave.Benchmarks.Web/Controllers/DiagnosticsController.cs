using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dave.Benchmarks.Core.Data;
using System.Data;
using System.Data.Common;

namespace Dave.Benchmarks.Web.Controllers;

/// <summary>
/// Provides diagnostic endpoints for checking system health.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : Controller
{
    private readonly BenchmarksDbContext context;

    /// <summary>
    /// Initializes a new instance of the DiagnosticsController.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DiagnosticsController(BenchmarksDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Tests the database connection and returns version information.
    /// </summary>
    /// <returns>Connection status and database version.</returns>
    [HttpGet("db")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            bool canConnect = await CanConnectAsync();
            if (!canConnect)
            {
                return Json(new
                {
                    status = "Error",
                    error = "Unable to connect to the database",
                    provider = context.Database.ProviderName
                });
            }

            string version = "unknown";
            try
            {
                var connection = GetDbConnection();
                bool wasOpen = connection.State == ConnectionState.Open;

                if (!wasOpen)
                    await connection.OpenAsync();

                version = connection.ServerVersion;

                if (!wasOpen)
                    await connection.CloseAsync();
            }
            catch
            {
                // Some providers may not expose a server version consistently.
                version = "unknown";
            }

            return Json(new { 
                status = "Connected",
                version = version,
                provider = context.Database.ProviderName
            });
        }
        catch (Exception ex)
        {
            return Json(new { 
                status = "Error",
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    protected virtual Task<bool> CanConnectAsync()
    {
        return context.Database.CanConnectAsync();
    }

    protected virtual DbConnection GetDbConnection()
    {
        return context.Database.GetDbConnection();
    }
}
