using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingApp.Tests.Helpers;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Cart;

namespace ShoppingApp.Tests.Controllers;

public class CartControllerTests
{
    private readonly Mock<ICartService> _svcMock = new();
    private readonly CartController _sut;

    private static CartReadDto FakeCart(int userId = 1) => new()
    {
        Id = 1, UserId = userId, Items = [], SubTotal = 0
    };

    private static IActionResult Unwrap<T>(ActionResult<T> ar) => ar.Result ?? new OkObjectResult(ar.Value);

    public CartControllerTests()
    {
        _sut = new CartController(_svcMock.Object);
        ControllerTestHelper.SetUser(_sut, userId: 1);
    }

    // ── GetMyCart ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyCart_AuthenticatedUser_Returns200()
    {
        _svcMock.Setup(s => s.GetByUserIdAsync(1, default)).ReturnsAsync(FakeCart());

        var ar = await _sut.GetMyCart(default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetMyCart_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var ar = await _sut.GetMyCart(default);

        Assert.IsType<UnauthorizedResult>(ar.Result);
    }

    [Fact]
    public async Task GetMyCart_ReturnsCartDto()
    {
        var cart = FakeCart(1);
        _svcMock.Setup(s => s.GetByUserIdAsync(1, default)).ReturnsAsync(cart);

        var ar = await _sut.GetMyCart(default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(cart, result!.Value);
    }

    // ── AddItemMe ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddItemMe_ValidItem_Returns200()
    {
        var dto = new CartAddItemDto { ProductId = 5, Quantity = 2 };
        _svcMock.Setup(s => s.AddItemAsync(1, dto, default)).ReturnsAsync(FakeCart());

        var ar = await _sut.AddItemMe(dto, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task AddItemMe_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new CartAddItemDto { ProductId = 5, Quantity = 1 };

        var ar = await _sut.AddItemMe(dto, default);

        Assert.IsType<UnauthorizedResult>(ar.Result);
    }

    [Fact]
    public async Task AddItemMe_CallsServiceWithCorrectUserId()
    {
        ControllerTestHelper.SetUser(_sut, userId: 3);
        var dto = new CartAddItemDto { ProductId = 5, Quantity = 1 };
        _svcMock.Setup(s => s.AddItemAsync(3, dto, default)).ReturnsAsync(FakeCart(3));

        await _sut.AddItemMe(dto, default);

        _svcMock.Verify(s => s.AddItemAsync(3, dto, default), Times.Once);
    }

    // ── UpdateItemMe ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemMe_ValidItem_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new CartUpdateItemDto { ProductId = 5, Quantity = 3 };
        _svcMock.Setup(s => s.UpdateItemAsync(1, dto, default)).ReturnsAsync(FakeCart());

        var ar = await _sut.UpdateItemMe(dto, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task UpdateItemMe_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var dto = new CartUpdateItemDto { ProductId = 5, Quantity = 1 };

        var ar = await _sut.UpdateItemMe(dto, default);

        Assert.IsType<UnauthorizedResult>(ar.Result);
    }

    [Fact]
    public async Task UpdateItemMe_ItemNotFound_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var dto = new CartUpdateItemDto { ProductId = 99, Quantity = 1 };
        _svcMock.Setup(s => s.UpdateItemAsync(1, dto, default)).ThrowsAsync(new NotFoundException("Not found"));

        var ar = await _sut.UpdateItemMe(dto, default);
        var result = Unwrap(ar) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── RemoveItemMe ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveItemMe_ValidItem_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.RemoveItemAsync(1, 5, default)).Returns(Task.CompletedTask);

        var result = await _sut.RemoveItemMe(5, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task RemoveItemMe_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.RemoveItemMe(5, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RemoveItemMe_ItemNotFound_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.RemoveItemAsync(1, 99, default)).ThrowsAsync(new NotFoundException("Not found"));

        var result = await _sut.RemoveItemMe(99, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── ClearMe ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearMe_AuthenticatedUser_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.ClearAsync(1, default)).Returns(Task.CompletedTask);

        var result = await _sut.ClearMe(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task ClearMe_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.ClearMe(default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── Admin: GetByUserId ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserId_Admin_Returns200()
    {
        _svcMock.Setup(s => s.GetByUserIdAsync(5, default)).ReturnsAsync(FakeCart(5));

        var ar = await _sut.GetByUserId(5, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Admin: AddItem ────────────────────────────────────────────────────────

    [Fact]
    public async Task AddItem_Admin_Returns200()
    {
        var dto = new CartAddItemDto { ProductId = 5, Quantity = 1 };
        _svcMock.Setup(s => s.AddItemAsync(5, dto, default)).ReturnsAsync(FakeCart(5));

        var ar = await _sut.AddItem(5, dto, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Admin: UpdateItem ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItem_Admin_Returns200()
    {
        var dto = new CartUpdateItemDto { ProductId = 5, Quantity = 2 };
        _svcMock.Setup(s => s.UpdateItemAsync(5, dto, default)).ReturnsAsync(FakeCart(5));

        var ar = await _sut.UpdateItem(5, dto, default);
        var result = Unwrap(ar) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Admin: RemoveItem ─────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveItem_Admin_Returns200()
    {
        _svcMock.Setup(s => s.RemoveItemAsync(5, 10, default)).Returns(Task.CompletedTask);

        var result = await _sut.RemoveItem(5, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── Admin: Clear ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Clear_Admin_Returns200()
    {
        _svcMock.Setup(s => s.ClearAsync(5, default)).Returns(Task.CompletedTask);

        var result = await _sut.Clear(5, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }
}
