using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Services.Logging;

namespace ShoppingApp.Tests.Services;

public class DbLogWriterTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DbLogWriter _sut;

    public DbLogWriterTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new DbLogWriter(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── InfoAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task InfoAsync_WritesLogWithInfoLevel()
    {
        await _sut.InfoAsync("TestSource", "Info message");

        var log = _db.Logs.Single();
        Assert.Equal("Info", log.Level);
        Assert.Equal("Info message", log.Message);
        Assert.Equal("TestSource", log.Source);
    }

    [Fact]
    public async Task InfoAsync_WithOptionalFields_PersistsAll()
    {
        await _sut.InfoAsync("Src", "Msg", eventId: 42, correlationId: "corr-1", requestPath: "/api/test");

        var log = _db.Logs.Single();
        Assert.Equal(42, log.EventId);
        Assert.Equal("corr-1", log.CorrelationId);
        Assert.Equal("/api/test", log.RequestPath);
    }

    // ── WarnAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task WarnAsync_WritesLogWithWarnLevel()
    {
        await _sut.WarnAsync("WarnSource", "Warning message");

        var log = _db.Logs.Single();
        Assert.Equal("Warn", log.Level);
        Assert.Equal("Warning message", log.Message);
    }

    [Fact]
    public async Task WarnAsync_NoException_ExceptionFieldIsNull()
    {
        await _sut.WarnAsync("Src", "Warn");

        var log = _db.Logs.Single();
        Assert.Null(log.Exception);
    }

    // ── ErrorAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ErrorAsync_WritesLogWithErrorLevel()
    {
        await _sut.ErrorAsync("ErrSource", "Error message");

        var log = _db.Logs.Single();
        Assert.Equal("Error", log.Level);
        Assert.Equal("Error message", log.Message);
    }

    [Fact]
    public async Task ErrorAsync_WithException_PersistsExceptionMessage()
    {
        var ex = new InvalidOperationException("Something went wrong");

        await _sut.ErrorAsync("Src", "Error occurred", ex);

        var log = _db.Logs.Single();
        Assert.Equal("Something went wrong", log.Exception);
        Assert.NotNull(log.StackTrace);
    }

    [Fact]
    public async Task ErrorAsync_WithNestedInnerException_PersistsOuterMessage()
    {
        var inner = new ArgumentNullException("param");
        var outer = new InvalidOperationException("Outer error", inner);

        await _sut.ErrorAsync("Src", "Nested error", outer);

        var log = _db.Logs.Single();
        Assert.Equal("Outer error", log.Exception);
    }

    [Fact]
    public async Task ErrorAsync_NullException_ExceptionFieldIsNull()
    {
        await _sut.ErrorAsync("Src", "Error", null);

        var log = _db.Logs.Single();
        Assert.Null(log.Exception);
        Assert.Null(log.StackTrace);
    }

    // ── Multiple writes ───────────────────────────────────────────────────────

    [Fact]
    public async Task MultipleWrites_AllPersisted()
    {
        await _sut.InfoAsync("S", "msg1");
        await _sut.WarnAsync("S", "msg2");
        await _sut.ErrorAsync("S", "msg3");

        Assert.Equal(3, _db.Logs.Count());
    }

    [Fact]
    public async Task MultipleWrites_LevelsAreCorrect()
    {
        await _sut.InfoAsync("S", "info");
        await _sut.WarnAsync("S", "warn");
        await _sut.ErrorAsync("S", "error");

        var levels = _db.Logs.Select(l => l.Level).OrderBy(l => l).ToList();
        Assert.Equal(new[] { "Error", "Info", "Warn" }, levels);
    }

    // ── Silent failure ────────────────────────────────────────────────────────

    [Fact]
    public async Task WriteAsync_DbFailure_DoesNotThrow()
    {
        // Dispose the DB to simulate failure — should swallow the exception
        _db.Dispose();

        var ex = await Record.ExceptionAsync(() => _sut.InfoAsync("S", "msg"));

        Assert.Null(ex);
    }
}
