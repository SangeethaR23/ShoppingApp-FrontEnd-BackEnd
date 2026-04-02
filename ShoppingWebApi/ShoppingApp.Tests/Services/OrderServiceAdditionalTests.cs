using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Return;
using ShoppingWebApi.Models.enums;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

public class OrderServiceAdditionalTests
{
    private readonly Mock<IRepository<int, Order>>       _orderRepoMock       = new();
    private readonly Mock<IRepository<int, OrderItem>>   _orderItemRepoMock   = new();
    private readonly Mock<IRepository<int, CartItem>>    _cartItemRepoMock    = new();
    private readonly Mock<IRepository<int, Payment>>     _paymentRepoMock     = new();
    private readonly Mock<IRepository<int, Refund>>      _refundRepoMock      = new();
    private readonly Mock<IRepository<int, ReturnRequest>> _returnRepoMock    = new();
    private readonly Mock<IRepository<int, Inventory>>   _inventoryRepoMock   = new();
    private readonly Mock<IPromoService>                 _promoServiceMock    = new();
    private readonly Mock<IWalletService>                _walletServiceMock   = new();
    private readonly Mock<ILogWriter>                    _logWriterMock       = new();
    private readonly Mock<ILogger<OrderService>>         _loggerMock          = new();

    public OrderServiceAdditionalTests()
    {
        _logWriterMock.Setup(l => l.InfoAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _logWriterMock.Setup(l => l.ErrorAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Exception?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private AppDbContext CreateCtx()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static Order MakeOrder(int userId, string num, OrderStatus status,
        decimal total = 100m, decimal walletUsed = 0m, List<OrderItem>? items = null) => new Order
        {
            UserId = userId, OrderNumber = num, Status = status, Total = total,
            WalletUsed = walletUsed, ShipToName = "J", ShipToLine1 = "L",
            ShipToCity = "C", ShipToState = "S", ShipToPostalCode = "P", ShipToCountry = "I",
            Items = items ?? new List<OrderItem>()
        };

    private static Payment SeedPayment(AppDbContext ctx, int orderId, int userId, string type)
    {
        var p = new Payment { OrderId = orderId, UserId = userId, PaymentType = type, TotalAmount = 0m, CreatedAt = DateTime.UtcNow };
        ctx.Payments.Add(p); ctx.SaveChanges(); return p;
    }

    private OrderService BuildSut(AppDbContext ctx)
    {
        _orderRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Orders);
        var userRepo    = new Mock<IRepository<int, User>>();
        var addressRepo = new Mock<IRepository<int, Address>>();
        var cartRepo    = new Mock<IRepository<int, Carts>>();
        var invRepo     = new Mock<IRepository<int, Inventory>>();
        var retRepo     = new Mock<IRepository<int, ReturnRequest>>();

        userRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Users);
        addressRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Addresses);
        cartRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Carts);
        invRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Inventories);
        retRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.ReturnRequests);
        retRepo.Setup(r => r.Add(It.IsAny<ReturnRequest>())).Returns((ReturnRequest r) => _returnRepoMock.Object.Add(r));
        retRepo.Setup(r => r.Get(It.IsAny<int>())).Returns((int id) => _returnRepoMock.Object.Get(id));
        retRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<ReturnRequest>())).Returns((int id, ReturnRequest r) => _returnRepoMock.Object.Update(id, r));
        invRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).Returns((int id, Inventory inv) => _inventoryRepoMock.Object.Update(id, inv));

        return new OrderService(
            userRepo.Object, addressRepo.Object, cartRepo.Object, _cartItemRepoMock.Object,
            _orderRepoMock.Object, _orderItemRepoMock.Object, invRepo.Object,
            _paymentRepoMock.Object, _refundRepoMock.Object, retRepo.Object,
            _promoServiceMock.Object, _walletServiceMock.Object, ctx,
            _logWriterMock.Object, _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════
    // GET ALL (Admin paged)
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task GetAll_NoFilters_ReturnsAllOrders()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", OrderStatus.Pending),
            MakeOrder(2, "O2", OrderStatus.Delivered),
            MakeOrder(1, "O3", OrderStatus.Shipped));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(page: 1, size: 10);

        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetAll_FilterByStatus_ReturnsMatching()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", OrderStatus.Pending),
            MakeOrder(1, "O2", OrderStatus.Delivered));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(status: "Delivered", page: 1, size: 10);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAll_FilterByUserId_ReturnsOnlyThatUser()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", OrderStatus.Pending),
            MakeOrder(2, "O2", OrderStatus.Pending));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(userId: 1, page: 1, size: 10);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAll_FilterByDateRange_Works()
    {
        using var ctx = CreateCtx();
        var old = MakeOrder(1, "O1", OrderStatus.Pending);
        old.PlacedAtUtc = DateTime.UtcNow.AddDays(-10);
        var recent = MakeOrder(1, "O2", OrderStatus.Pending);
        recent.PlacedAtUtc = DateTime.UtcNow;
        ctx.Orders.AddRange(old, recent);
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(
            from: DateTime.UtcNow.AddDays(-1), page: 1, size: 10);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAll_SortByTotal_Works()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", OrderStatus.Pending, total: 500m),
            MakeOrder(1, "O2", OrderStatus.Pending, total: 100m));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(sortBy: "total", desc: false, page: 1, size: 10);

        Assert.Equal(100m, result.Items[0].Total);
    }

    [Fact]
    public async Task GetAll_SortByStatus_Works()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", OrderStatus.Shipped),
            MakeOrder(1, "O2", OrderStatus.Pending));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(sortBy: "status", desc: false, page: 1, size: 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAll_Pagination_Works()
    {
        using var ctx = CreateCtx();
        for (int i = 1; i <= 5; i++)
            ctx.Orders.Add(MakeOrder(1, $"O{i}", OrderStatus.Pending));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(page: 2, size: 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }

    // ═══════════════════════════════════════════════
    // GET USER ORDERS — sort variants
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task GetUserOrders_SortByTotal_Works()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", OrderStatus.Pending, total: 300m),
            MakeOrder(1, "O2", OrderStatus.Pending, total: 50m));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10, sortBy: "total", desc: false);

        Assert.Equal(50m, result.Items[0].Total);
    }

    [Fact]
    public async Task GetUserOrders_SortByStatus_Works()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", OrderStatus.Shipped),
            MakeOrder(1, "O2", OrderStatus.Pending));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10, sortBy: "status", desc: false);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetUserOrders_FilterByDateFrom_Works()
    {
        using var ctx = CreateCtx();
        var old = MakeOrder(1, "O1", OrderStatus.Pending);
        old.PlacedAtUtc = DateTime.UtcNow.AddDays(-5);
        var recent = MakeOrder(1, "O2", OrderStatus.Pending);
        recent.PlacedAtUtc = DateTime.UtcNow;
        ctx.Orders.AddRange(old, recent);
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10,
            from: DateTime.UtcNow.AddDays(-1));

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetUserOrders_FilterByDateTo_Works()
    {
        using var ctx = CreateCtx();
        var old = MakeOrder(1, "O1", OrderStatus.Pending);
        old.PlacedAtUtc = DateTime.UtcNow.AddDays(-5);
        var recent = MakeOrder(1, "O2", OrderStatus.Pending);
        recent.PlacedAtUtc = DateTime.UtcNow;
        ctx.Orders.AddRange(old, recent);
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10,
            to: DateTime.UtcNow.AddDays(-2));

        Assert.Single(result.Items);
    }

    // ═══════════════════════════════════════════════
    // UPDATE STATUS → Cancelled path
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatus_ToCancelled_TriggersCancelFlow()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Pending, total: 100m,
            items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, ProductName = "P", SKU = "S", UnitPrice = 100m, LineTotal = 100m } }));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "UPI");

        _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
        _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _refundRepoMock.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());
        _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((100m, 1));

        var result = await BuildSut(ctx).UpdateStatusAsync(order.Id, "Cancelled");

        Assert.True(result);
    }

    // ═══════════════════════════════════════════════
    // REVIEW RETURN — Reject
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task ReviewReturn_NotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        _returnRepoMock.Setup(r => r.Get(99)).ReturnsAsync((ReturnRequest?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildSut(ctx).ReviewReturnAsync(99, new ReturnRequestUpdateDto { Action = "approve" }));
    }

    [Fact]
    public async Task ReviewReturn_Reject_UpdatesStatusToRejected()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested, total: 100m));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "UPI");

        var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested, Reason = "Damaged" };
        _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
        _returnRepoMock.Setup(r => r.Update(1, It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
        _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);

        var result = await BuildSut(ctx).ReviewReturnAsync(1,
            new ReturnRequestUpdateDto { Action = "reject", Comments = "Not eligible" });

        Assert.True(result);
        Assert.Equal(ReturnStatus.Rejected, returnReq.Status);
    }

    // ═══════════════════════════════════════════════
    // REVIEW RETURN — Approve COD (no refund)
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task ReviewReturn_ApproveCOD_NoWalletRefund()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 5 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested, total: 200m,
            items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2, ProductName = "P", SKU = "S", UnitPrice = 100m, LineTotal = 200m } }));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "cod");

        var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested, Reason = "Damaged" };
        _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
        _returnRepoMock.Setup(r => r.Update(1, It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
        _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);

        var result = await BuildSut(ctx).ReviewReturnAsync(1,
            new ReturnRequestUpdateDto { Action = "approve" });

        Assert.True(result);
        // No wallet credit for COD
        _walletServiceMock.Verify(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ═══════════════════════════════════════════════
    // REVIEW RETURN — Approve Online (wallet refund)
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task ReviewReturn_ApproveOnline_CreditsWallet()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 5 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested, total: 300m,
            items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, ProductName = "P", SKU = "S", UnitPrice = 300m, LineTotal = 300m } }));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "UPI");

        var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested, Reason = "Wrong item" };
        _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
        _returnRepoMock.Setup(r => r.Update(1, It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
        _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
        _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((300m, 1));

        var result = await BuildSut(ctx).ReviewReturnAsync(1,
            new ReturnRequestUpdateDto { Action = "approve" });

        Assert.True(result);
        _walletServiceMock.Verify(w => w.CreditAsync(order.UserId, 300m, WalletTxnType.CreditRefund,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewReturn_ApproveOnline_RestoresInventory()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 3 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested, total: 100m,
            items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2, ProductName = "P", SKU = "S", UnitPrice = 50m, LineTotal = 100m } }));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "UPI");

        var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested, Reason = "Defective" };
        _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
        _returnRepoMock.Setup(r => r.Update(1, It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
        _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
        _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((100m, 1));

        await BuildSut(ctx).ReviewReturnAsync(1, new ReturnRequestUpdateDto { Action = "approve" });

        _inventoryRepoMock.Verify(r => r.Update(inv.Id, It.Is<Inventory>(i => i.Quantity == 5)), Times.Once);
    }
}
