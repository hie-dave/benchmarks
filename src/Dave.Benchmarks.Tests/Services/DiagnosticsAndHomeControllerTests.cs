using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Tests.Helpers;
using Dave.Benchmarks.Web.Controllers;
using Dave.Benchmarks.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Data;
using System.Data.Common;

namespace Dave.Benchmarks.Tests.Services;

public class DiagnosticsAndHomeControllerTests
{
    [Fact]
    public async Task TestDatabase_SucceedsWithValidConnection()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        DiagnosticsController controller = new(db);

        IActionResult result = await controller.TestDatabase();

        JsonResult json = Assert.IsType<JsonResult>(result);
        Assert.Equal("Connected", ReadStringProperty(json.Value!, "status"));
        Assert.False(string.IsNullOrWhiteSpace(ReadStringProperty(json.Value!, "provider")));
    }

    [Fact]
    public async Task TestDatabase_WhenCanConnectIsFalse_ReturnsErrorJson()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        TestableDiagnosticsController controller = new(
            db,
            canConnectAsync: () => Task.FromResult(false),
            getDbConnection: () => new FakeDbConnection(ConnectionState.Closed, "fake-v1"));

        IActionResult result = await controller.TestDatabase();

        JsonResult json = Assert.IsType<JsonResult>(result);
        Assert.Equal("Error", ReadStringProperty(json.Value!, "status"));
        Assert.Contains("Unable to connect", ReadStringProperty(json.Value!, "error"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestDatabase_WhenConnectionInitiallyClosed_OpensAndClosesConnection()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        FakeDbConnection connection = new(ConnectionState.Closed, "fake-v2");
        TestableDiagnosticsController controller = new(
            db,
            canConnectAsync: () => Task.FromResult(true),
            getDbConnection: () => connection);

        IActionResult result = await controller.TestDatabase();

        JsonResult json = Assert.IsType<JsonResult>(result);
        Assert.Equal("Connected", ReadStringProperty(json.Value!, "status"));
        Assert.Equal("fake-v2", ReadStringProperty(json.Value!, "version"));
        Assert.Equal(1, connection.OpenCount);
        Assert.Equal(1, connection.CloseCount);
    }

    [Fact]
    public async Task TestDatabase_WhenCanConnectThrows_ReturnsErrorJson()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        using BenchmarksDbContext db = fixture.CreateContext();
        TestableDiagnosticsController controller = new(
            db,
            canConnectAsync: () => Task.FromException<bool>(new InvalidOperationException("boom")),
            getDbConnection: () => new FakeDbConnection(ConnectionState.Closed, "fake-v1"));

        IActionResult result = await controller.TestDatabase();

        JsonResult json = Assert.IsType<JsonResult>(result);
        Assert.Equal("Error", ReadStringProperty(json.Value!, "status"));
        Assert.Contains("boom", ReadStringProperty(json.Value!, "error"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestDatabase_WhenContextDisposed_ReturnsErrorJson()
    {
        using SqliteTestDb fixture = SqliteTestDb.Create();
        BenchmarksDbContext db = fixture.CreateContext();
        DiagnosticsController controller = new(db);
        db.Dispose();

        IActionResult result = await controller.TestDatabase();

        JsonResult json = Assert.IsType<JsonResult>(result);
        Assert.Equal("Error", ReadStringProperty(json.Value!, "status"));
        string error = ReadStringProperty(json.Value!, "error");
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Contains("disposed", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Index_ReturnsView()
    {
        HomeController controller = new(Mock.Of<ILogger<HomeController>>());

        IActionResult result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewWithTraceIdentifier()
    {
        HomeController controller = new(Mock.Of<ILogger<HomeController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "trace-123"
                }
            }
        };

        IActionResult result = controller.Error();

        ViewResult view = Assert.IsType<ViewResult>(result);
        ErrorViewModel model = Assert.IsType<ErrorViewModel>(view.Model);
        Assert.Equal("trace-123", model.RequestId);
        Assert.True(model.ShowRequestId);
    }

    private static string ReadStringProperty(object value, string propertyName)
    {
        object? prop = value.GetType().GetProperty(propertyName)?.GetValue(value);
        return prop?.ToString() ?? string.Empty;
    }

    private sealed class TestableDiagnosticsController : DiagnosticsController
    {
        private readonly Func<Task<bool>> canConnectAsync;
        private readonly Func<DbConnection> getDbConnection;

        public TestableDiagnosticsController(
            BenchmarksDbContext context,
            Func<Task<bool>> canConnectAsync,
            Func<DbConnection> getDbConnection)
            : base(context)
        {
            this.canConnectAsync = canConnectAsync;
            this.getDbConnection = getDbConnection;
        }

        protected override Task<bool> CanConnectAsync() => canConnectAsync();

        protected override DbConnection GetDbConnection() => getDbConnection();
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState state;
        private readonly string serverVersion;

        public int OpenCount { get; private set; }
        public int CloseCount { get; private set; }

        public FakeDbConnection(ConnectionState initialState, string serverVersion)
        {
            state = initialState;
            this.serverVersion = serverVersion;
        }

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "fake";

        public override string DataSource => "fake";

        public override string ServerVersion => serverVersion;

        public override ConnectionState State => state;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Close()
        {
            state = ConnectionState.Closed;
            CloseCount++;
        }

        public override void Open()
        {
            state = ConnectionState.Open;
            OpenCount++;
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            Open();
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
