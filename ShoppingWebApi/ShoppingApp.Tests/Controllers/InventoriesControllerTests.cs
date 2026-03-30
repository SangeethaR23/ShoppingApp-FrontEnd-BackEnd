using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Inventory;

namespace ShoppingApp.Tests.Controllers;

public class InventoriesControllerTests
{
    private readonly Mock<IInventoryService> _svcMock = new();
    private readonly InventoriesController _sut;

    private static InventoryReadDto FakeInventory(int id = 1, int productId = 10) => new()
    {
        Id = id, ProductId = productId, ProductName = "Widget", SKU = "W-001",
        Quantity = 50, ReorderLevel = 10, CreatedUtc = DateTime.UtcNow
    };

    private static PagedResult<InventoryReadDto> FakePage() => new()
    {
        Items = [FakeInventory()], TotalCount = 1, PageNumber = 1, PageSize = 10
    };

    public InventoriesControllerTests() => _sut = new InventoriesController(_svcMock.Object);

    // ── GetPaged ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_Returns200()
    {
        _svcMock.Setup(s => s.GetPagedAsync(null, null, null, null, "product", false, 1, 10, default))
            .ReturnsAsync(FakePage());

        var result = await _sut.GetPaged(null, null, null, null, "product", false, 1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithFilters_Returns200()
    {
        _svcMock.Setup(s => s.GetPagedAsync(10, null, null, true, "product", false, 1, 10, default))
            .ReturnsAsync(FakePage());

        var result = await _sut.GetPaged(10, null, null, true, "product", false, 1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetPaged_EmptyResult_Returns200()
    {
        _svcMock.Setup(s => s.GetPagedAsync(null, null, null, null, "product", false, 1, 10, default))
            .ReturnsAsync(new PagedResult<InventoryReadDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 });

        var result = await _sut.GetPaged(null, null, null, null, "product", false, 1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        _svcMock.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(FakeInventory());

        var result = await _sut.GetById(1, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, default)).ReturnsAsync((InventoryReadDto?)null);

        var result = await _sut.GetById(99, default);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetByProduct ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByProduct_ExistingProduct_Returns200()
    {
        _svcMock.Setup(s => s.GetByProductIdAsync(10, default)).ReturnsAsync(FakeInventory(productId: 10));

        var result = await _sut.GetByProduct(10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetByProduct_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.GetByProductIdAsync(99, default)).ReturnsAsync((InventoryReadDto?)null);

        var result = await _sut.GetByProduct(99, default);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Adjust ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Adjust_PositiveDelta_Returns200()
    {
        var dto = new InventoryAdjustRequestDto { Delta = 10, Reason = "Restock" };
        _svcMock.Setup(s => s.AdjustAsync(10, 10, "Restock", default)).ReturnsAsync(FakeInventory());

        var result = await _sut.Adjust(10, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Adjust_NegativeDelta_Returns200()
    {
        var dto = new InventoryAdjustRequestDto { Delta = -5, Reason = "Sold" };
        _svcMock.Setup(s => s.AdjustAsync(10, -5, "Sold", default)).ReturnsAsync(FakeInventory());

        var result = await _sut.Adjust(10, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Adjust_CallsServiceWithCorrectArgs()
    {
        var dto = new InventoryAdjustRequestDto { Delta = 20, Reason = "Audit" };
        _svcMock.Setup(s => s.AdjustAsync(10, 20, "Audit", default)).ReturnsAsync(FakeInventory());

        await _sut.Adjust(10, dto, default);

        _svcMock.Verify(s => s.AdjustAsync(10, 20, "Audit", default), Times.Once);
    }

    // ── SetQuantity ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SetQuantity_ValidRequest_Returns200()
    {
        var dto = new InventorySetRequestDto { Quantity = 100 };
        _svcMock.Setup(s => s.SetQuantityAsync(10, 100, default)).ReturnsAsync(FakeInventory());

        var result = await _sut.SetQuantity(10, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task SetQuantity_ZeroQuantity_Returns200()
    {
        var dto = new InventorySetRequestDto { Quantity = 0 };
        _svcMock.Setup(s => s.SetQuantityAsync(10, 0, default)).ReturnsAsync(FakeInventory());

        var result = await _sut.SetQuantity(10, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── SetReorderLevel ───────────────────────────────────────────────────────

    [Fact]
    public async Task SetReorderLevel_ValidRequest_Returns200()
    {
        var dto = new InventoryReorderLevelRequestDto { ReorderLevel = 15 };
        _svcMock.Setup(s => s.SetReorderLevelAsync(10, 15, default)).ReturnsAsync(FakeInventory());

        var result = await _sut.SetReorderLevel(10, dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task SetReorderLevel_CallsServiceWithCorrectArgs()
    {
        var dto = new InventoryReorderLevelRequestDto { ReorderLevel = 5 };
        _svcMock.Setup(s => s.SetReorderLevelAsync(10, 5, default)).ReturnsAsync(FakeInventory());

        await _sut.SetReorderLevel(10, dto, default);

        _svcMock.Verify(s => s.SetReorderLevelAsync(10, 5, default), Times.Once);
    }
}
