using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingApp.Tests.Helpers;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Return;

namespace ShoppingApp.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _svcMock = new();
    private readonly OrdersController _sut;

    private static OrderReadDto FakeOrder(int id = 1) => new()
    {
        Id = id, OrderNumber = "ORD-001", Status = "Pending", PaymentStatus = "Pending",
        PlacedAtUtc = DateTime.UtcNow, ShipToName = "Test User",
        ShipToLine1 = "123 St", ShipToCity = "City", ShipToState = "ST",
        ShipToPostalCode = "12345", ShipToCountry = "IN",
        SubTotal = 100, ShippingFee = 10, Discount = 0, Total = 110
    };

    private static PlaceOrderResponseDto FakePlaceResponse() => new()
    {
        Id = 1, OrderNumber = "ORD-001", Total = 110, Status = "Pending", PaymentStatus = "Pending"
    };

    public OrdersControllerTests()
    {
        _sut = new OrdersController(_svcMock.Object);
        ControllerTestHelper.SetUser(_sut, userId: 1);
    }

    // ── Place ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Place_ValidRequest_Returns201()
    {
        var req = new PlaceOrderRequestDto { AddressId = 1, PaymentType = "CashOnDelivery" };
        _svcMock.Setup(s => s.PlaceOrderAsync(It.IsAny<PlaceOrderRequestDto>(), default))
            .ReturnsAsync(FakePlaceResponse());

        var result = await _sut.Place(req, default) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task Place_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var req = new PlaceOrderRequestDto { AddressId = 1, PaymentType = "CashOnDelivery" };

        var result = await _sut.Place(req, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Place_InsufficientInventory_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var req = new PlaceOrderRequestDto { AddressId = 1, PaymentType = "CashOnDelivery" };
        _svcMock.Setup(s => s.PlaceOrderAsync(It.IsAny<PlaceOrderRequestDto>(), default))
            .ThrowsAsync(new BusinessValidationException("Insufficient inventory"));

        var result = await _sut.Place(req, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Place_SetsUserIdFromClaims()
    {
        ControllerTestHelper.SetUser(_sut, userId: 7);
        var req = new PlaceOrderRequestDto { AddressId = 1, PaymentType = "CashOnDelivery" };
        PlaceOrderRequestDto? captured = null;
        _svcMock.Setup(s => s.PlaceOrderAsync(It.IsAny<PlaceOrderRequestDto>(), default))
            .Callback<PlaceOrderRequestDto, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(FakePlaceResponse());

        await _sut.Place(req, default);

        Assert.Equal(7, captured!.UserId);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingOrder_Returns200()
    {
        _svcMock.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(FakeOrder());

        var result = await _sut.GetById(1, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, default)).ReturnsAsync((OrderReadDto?)null);

        var result = await _sut.GetById(99, default);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetMine ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMine_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var page = new PagedResult<OrderSummaryDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 };
        _svcMock.Setup(s => s.GetUserOrdersAsync(1, 1, 10, null, false, null, null, null, default))
            .ReturnsAsync(page);
        var req = new OrderPagedRequestDto { Page = 1, Size = 10 };

        var result = await _sut.GetMine(req, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetMine_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);
        var req = new OrderPagedRequestDto { Page = 1, Size = 10 };

        var result = await _sut.GetMine(req, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_ValidOrder_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        var cancelResponse = new CancelOrderResponseDto { Id = 1, Status = "Cancelled", Message = "Cancelled" };
        _svcMock.Setup(s => s.CancelOrderAsync(1, 1, false, null, default)).ReturnsAsync(cancelResponse);

        var result = await _sut.Cancel(1, null, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Cancel_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.Cancel(1, null, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Cancel_ConflictException_Rethrows()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.CancelOrderAsync(1, 1, false, null, default))
            .ThrowsAsync(new ConflictException("Cannot cancel"));

        await Assert.ThrowsAsync<ConflictException>(() => _sut.Cancel(1, null, default));
    }

    // ── Return ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Return_ValidOrder_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.RequestReturnAsync(1, It.IsAny<ReturnRequestCreateDto>(), default))
            .ReturnsAsync(true);

        var result = await _sut.Return(1, "Defective", default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Return_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.Return(1, null, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Return_OrderNotFound_Returns404()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.RequestReturnAsync(1, It.IsAny<ReturnRequestCreateDto>(), default))
            .ThrowsAsync(new NotFoundException("Order not found"));

        var result = await _sut.Return(1, null, default) as NotFoundObjectResult;

        Assert.Equal(404, result!.StatusCode);
    }

    [Fact]
    public async Task Return_BusinessValidationFails_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.RequestReturnAsync(1, It.IsAny<ReturnRequestCreateDto>(), default))
            .ThrowsAsync(new BusinessValidationException("Order not delivered"));

        var result = await _sut.Return(1, null, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    // ── GetPaged (Admin) ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_Returns200()
    {
        var page = new PagedResult<OrderReadDto> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 };
        _svcMock.Setup(s => s.GetAllAsync(null, null, null, null, 1, 10, null, false, default))
            .ReturnsAsync(page);
        var req = new OrderPagedRequestDto { Page = 1, Size = 10 };

        var result = await _sut.GetPaged(req, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── UpdateStatus (Admin) ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidStatus_Returns200()
    {
        _svcMock.Setup(s => s.UpdateStatusAsync(1, "Shipped", default)).ReturnsAsync(true);
        var req = new UpdateOrderStatusRequset { Status = "Shipped" };

        var result = await _sut.UpdateStatus(1, req, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_Returns400()
    {
        _svcMock.Setup(s => s.UpdateStatusAsync(99, "Invalid", default)).ReturnsAsync(false);
        var req = new UpdateOrderStatusRequset { Status = "Invalid" };

        var result = await _sut.UpdateStatus(99, req, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }
}
