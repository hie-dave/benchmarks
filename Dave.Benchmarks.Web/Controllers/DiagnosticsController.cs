using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dave.Benchmarks.Core.Data;

namespace Dave.Benchmarks.Web.Controllers;

/// <summary>
/// Provides diagnostic endpoints for checking system health.
/// </summary>
public class DiagnosticsController : Controller
{
    private readonly BenchmarksContext context;

    /// <summary>
    /// Initializes a new instance of the DiagnosticsController.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DiagnosticsController(BenchmarksContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Tests the database connection and returns version information.
    /// </summary>
    /// <returns>Connection status and database version.</returns>
    [HttpGet("api/diagnostics/db")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            string version = await context.Database
                .SqlQuery<string>($"SELECT VERSION() AS Value")
                .FirstOrDefaultAsync() ?? "unknown";

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
}
