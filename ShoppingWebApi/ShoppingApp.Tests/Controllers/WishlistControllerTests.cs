using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingApp.Tests.Helpers;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Wishlist;
using System.Security.Claims;

namespace ShoppingApp.Tests.Controllers;

public class WishlistControllerTests
{
    private readonly Mock<IWishlistService> _svcMock = new();
    private readonly WishlistController _sut;

    private static WishlistReadDto FakeItem() => new()
    {
        ProductId = 10, ProductName = "Widget", SKU = "W-001", Price = 99, IsActive = true
    };

    public WishlistControllerTests()
    {
        _sut = new WishlistController(_svcMock.Object);
        SetUser(1);
    }

    // WishlistController uses User.FindFirst("userId") directly, not GetUserId()
    private void SetUser(int userId)
    {
        var claims = new[] { new Claim("userId", userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Toggle_Added_Returns200WithAddedTrue()
    {
        _svcMock.Setup(s => s.ToggleAsync(1, 10, default)).ReturnsAsync(true);
        var dto = new WishlistToggleDto { ProductId = 10 };

        var result = await _sut.Toggle(dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
        dynamic val = result.Value!;
        Assert.True((bool)val.GetType().GetProperty("added")!.GetValue(val)!);
    }

    [Fact]
    public async Task Toggle_Removed_Returns200WithAddedFalse()
    {
        _svcMock.Setup(s => s.ToggleAsync(1, 10, default)).ReturnsAsync(false);
        var dto = new WishlistToggleDto { ProductId = 10 };

        var result = await _sut.Toggle(dto, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Toggle_CallsServiceWithCorrectUserId()
    {
        SetUser(5);
        _svcMock.Setup(s => s.ToggleAsync(5, 10, default)).ReturnsAsync(true);
        var dto = new WishlistToggleDto { ProductId = 10 };

        await _sut.Toggle(dto, default);

        _svcMock.Verify(s => s.ToggleAsync(5, 10, default), Times.Once);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithItems_Returns200()
    {
        _svcMock.Setup(s => s.GetAsync(1, default)).ReturnsAsync(new[] { FakeItem() });

        var result = await _sut.Get(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Get_EmptyWishlist_Returns200()
    {
        _svcMock.Setup(s => s.GetAsync(1, default)).ReturnsAsync(Enumerable.Empty<WishlistReadDto>());

        var result = await _sut.Get(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Get_CallsServiceWithCorrectUserId()
    {
        SetUser(7);
        _svcMock.Setup(s => s.GetAsync(7, default)).ReturnsAsync(Enumerable.Empty<WishlistReadDto>());

        await _sut.Get(default);

        _svcMock.Verify(s => s.GetAsync(7, default), Times.Once);
    }

    // ── MoveToCart ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveToCart_ValidProduct_Returns200()
    {
        _svcMock.Setup(s => s.MoveToCartAsync(1, 10, default)).ReturnsAsync(true);

        var result = await _sut.MoveToCart(10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task MoveToCart_CallsServiceWithCorrectIds()
    {
        SetUser(3);
        _svcMock.Setup(s => s.MoveToCartAsync(3, 20, default)).ReturnsAsync(true);

        await _sut.MoveToCart(20, default);

        _svcMock.Verify(s => s.MoveToCartAsync(3, 20, default), Times.Once);
    }
}
