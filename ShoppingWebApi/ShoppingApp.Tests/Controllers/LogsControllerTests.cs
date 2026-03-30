using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Mappings;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.AdminLogs;
using ShoppingWebApi.Models.DTOs.Common;

namespace ShoppingApp.Tests.Controllers;

public class LogsControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly LogsController _sut;

    public LogsControllerTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        var mapConfig = new MapperConfiguration(cfg => cfg.AddProfile<AppMappingProfile>());
        _mapper = mapConfig.CreateMapper();

        _sut = new LogsController(_db, _mapper);

        // Seed log entries
        _db.Logs.AddRange(
            new LogEntry { Id = 1, Level = "Error",   Message = "NullRef in OrderService",  Source = "OrderService",   CreatedUtc = DateTime.UtcNow.AddHours(-2) },
            new LogEntry { Id = 2, Level = "Warning", Message = "Slow query detected",       Source = "ProductService", CreatedUtc = DateTime.UtcNow.AddHours(-1) },
            new LogEntry { Id = 3, Level = "Info",    Message = "User registered",           Source = "AuthService",    CreatedUtc = DateTime.UtcNow },
            new LogEntry { Id = 4, Level = "Error",   Message = "DB timeout",                Source = "OrderService",   Exception = "TimeoutException", CreatedUtc = DateTime.UtcNow.AddMinutes(-30) }
        );
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // ── No filters ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_NoFilters_ReturnsAllLogs()
    {
        var query = new LogQueryDto { Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(4, page!.TotalCount);
    }

    // ── Level filter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_FilterByLevel_ReturnsOnlyMatchingLevel()
    {
        var query = new LogQueryDto { Level = "Error", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Equal(2, page!.TotalCount);
        Assert.All(page.Items, l => Assert.Equal("Error", l.Level));
    }

    [Fact]
    public async Task SearchLogs_FilterByInfo_ReturnsSingleLog()
    {
        var query = new LogQueryDto { Level = "Info", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Equal(1, page!.TotalCount);
    }

    // ── Source filter ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_FilterBySource_ReturnsOnlyMatchingSource()
    {
        var query = new LogQueryDto { Source = "OrderService", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Equal(2, page!.TotalCount);
        Assert.All(page.Items, l => Assert.Equal("OrderService", l.Source));
    }

    // ── Keyword search ────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_KeywordInMessage_ReturnsMatches()
    {
        var query = new LogQueryDto { Search = "timeout", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        // "DB timeout" in message + "TimeoutException" in exception
        Assert.True(page!.TotalCount >= 1);
    }

    [Fact]
    public async Task SearchLogs_KeywordInException_ReturnsMatches()
    {
        var query = new LogQueryDto { Search = "TimeoutException", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.True(page!.TotalCount >= 1);
    }

    [Fact]
    public async Task SearchLogs_NoMatchingKeyword_ReturnsEmpty()
    {
        var query = new LogQueryDto { Search = "xyznotfound", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Equal(0, page!.TotalCount);
    }

    // ── Date range filter ─────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_FromFilter_ExcludesOlderLogs()
    {
        var query = new LogQueryDto { From = DateTime.UtcNow.AddMinutes(-45), Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        // Only logs from last 45 min: id=3 (now) and id=4 (-30min)
        Assert.Equal(2, page!.TotalCount);
    }

    [Fact]
    public async Task SearchLogs_ToFilter_ExcludesNewerLogs()
    {
        var query = new LogQueryDto { To = DateTime.UtcNow.AddHours(-1).AddMinutes(-1), Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        // Only id=1 (-2h)
        Assert.Equal(1, page!.TotalCount);
    }

    // ── Sorting ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_SortByDateDesc_ReturnsNewestFirst()
    {
        var query = new LogQueryDto { SortBy = "date", SortDir = "desc", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        var dates = page!.Items.Select(l => l.CreatedUtc).ToList();
        Assert.Equal(dates.OrderByDescending(d => d).ToList(), dates);
    }

    [Fact]
    public async Task SearchLogs_SortByLevel_Returns200()
    {
        var query = new LogQueryDto { SortBy = "level", SortDir = "asc", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task SearchLogs_SortBySource_Returns200()
    {
        var query = new LogQueryDto { SortBy = "source", SortDir = "desc", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_PageSize1_ReturnsOnlyOneItem()
    {
        var query = new LogQueryDto { Page = 1, Size = 1 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Single(page!.Items);
        Assert.Equal(4, page.TotalCount);
    }

    [Fact]
    public async Task SearchLogs_Page2_ReturnsCorrectItems()
    {
        var query = new LogQueryDto { Page = 2, Size = 2 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Equal(2, page!.Items.Count);
    }

    [Fact]
    public async Task SearchLogs_PageBeyondData_ReturnsEmpty()
    {
        var query = new LogQueryDto { Page = 99, Size = 10 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Empty(page!.Items);
        Assert.Equal(4, page.TotalCount);
    }

    // ── Combined filters ──────────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogs_LevelAndSource_ReturnsIntersection()
    {
        var query = new LogQueryDto { Level = "Error", Source = "OrderService", Page = 1, Size = 20 };

        var ar = await _sut.SearchLogs(query, default);
        var result = ar.Result as OkObjectResult;
        var page = result!.Value as PagedResult<LogEntryReadDto>;

        Assert.Equal(2, page!.TotalCount);
    }
}
