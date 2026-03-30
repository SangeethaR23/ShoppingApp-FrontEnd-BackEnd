using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Promo;

namespace ShoppingApp.Tests.Controllers;

public class PromoControllerTests
{
    private readonly Mock<IPromoService> _svcMock = new();
    private readonly PromoController _sut;

    private static PromoCode FakePromoCode(int id = 1) => new()
    {
        Id = id, Code = "SAVE10", DiscountAmount = 10, IsActive = true,
        MinOrderAmount = 100, StartDateUtc = DateTime.UtcNow.AddDays(-1),
        EndDateUtc = DateTime.UtcNow.AddDays(30)
    };

    public PromoControllerTests() => _sut = new PromoController(_svcMock.Object);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns200WithPromo()
    {
        var dto = new PromoCreateDto
        {
            Code = "SAVE10", DiscountAmount = 10,
            StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(30)
        };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ReturnsAsync(FakePromoCode());

        var result = await _sut.Create(dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
        Assert.IsType<PromoCode>(result.Value);
    }

    [Fact]
    public async Task Create_CallsServiceOnce()
    {
        var dto = new PromoCreateDto { Code = "TEST", DiscountAmount = 5, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(1) };
        _svcMock.Setup(s => s.CreateAsync(dto, default)).ReturnsAsync(FakePromoCode());

        await _sut.Create(dto, default);

        _svcMock.Verify(s => s.CreateAsync(dto, default), Times.Once);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_ExistingPromo_Returns200()
    {
        _svcMock.Setup(s => s.ActivateAsync(1, true, default)).ReturnsAsync(true);

        var result = await _sut.Activate(1, true, default) as OkResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Activate_Deactivate_Returns200()
    {
        _svcMock.Setup(s => s.ActivateAsync(1, false, default)).ReturnsAsync(true);

        var result = await _sut.Activate(1, false, default) as OkResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Activate_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.ActivateAsync(99, true, default)).ReturnsAsync(false);

        var result = await _sut.Activate(99, true, default);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithList()
    {
        _svcMock.Setup(s => s.GetAllAsync(default)).ReturnsAsync(new[] { FakePromoCode(), FakePromoCode(2) });

        var result = await _sut.GetAll(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetAll_EmptyList_Returns200()
    {
        _svcMock.Setup(s => s.GetAllAsync(default)).ReturnsAsync(Enumerable.Empty<PromoCode>());

        var result = await _sut.GetAll(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsMappedDtos()
    {
        _svcMock.Setup(s => s.GetAllAsync(default)).ReturnsAsync(new[] { FakePromoCode() });

        var result = await _sut.GetAll(default) as OkObjectResult;
        var items = (result!.Value as IEnumerable<PromoReadDto>)!.ToList();

        Assert.Single(items);
        Assert.Equal("SAVE10", items[0].Code);
    }

    // ── ApplyPromo ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyPromo_ValidCode_Returns200WithDiscount()
    {
        var dto = new ApplyPromoDto { PromoCode = "save10", CartTotal = 200 };
        _svcMock.Setup(s => s.GetValidPromoAsync("SAVE10", 200, default)).ReturnsAsync(FakePromoCode());

        var result = await _sut.ApplyPromo(dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
        Assert.IsType<PromoReadDto>(result.Value);
    }

    [Fact]
    public async Task ApplyPromo_InvalidCode_Returns400()
    {
        var dto = new ApplyPromoDto { PromoCode = "INVALID", CartTotal = 200 };
        _svcMock.Setup(s => s.GetValidPromoAsync("INVALID", 200, default)).ReturnsAsync((PromoCode?)null);

        var result = await _sut.ApplyPromo(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task ApplyPromo_ExpiredCode_Returns400()
    {
        var dto = new ApplyPromoDto { PromoCode = "EXPIRED", CartTotal = 200 };
        _svcMock.Setup(s => s.GetValidPromoAsync("EXPIRED", 200, default)).ReturnsAsync((PromoCode?)null);

        var result = await _sut.ApplyPromo(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task ApplyPromo_CartTotalBelowMinimum_Returns400()
    {
        var dto = new ApplyPromoDto { PromoCode = "SAVE10", CartTotal = 50 };
        _svcMock.Setup(s => s.GetValidPromoAsync("SAVE10", 50, default)).ReturnsAsync((PromoCode?)null);

        var result = await _sut.ApplyPromo(dto, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task ApplyPromo_UppercasesCode()
    {
        var dto = new ApplyPromoDto { PromoCode = "save10", CartTotal = 200 };
        _svcMock.Setup(s => s.GetValidPromoAsync("SAVE10", 200, default)).ReturnsAsync(FakePromoCode());

        await _sut.ApplyPromo(dto, default);

        _svcMock.Verify(s => s.GetValidPromoAsync("SAVE10", 200, default), Times.Once);
    }
}
