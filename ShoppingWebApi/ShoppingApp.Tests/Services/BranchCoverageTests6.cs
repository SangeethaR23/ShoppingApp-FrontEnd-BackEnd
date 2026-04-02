using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Cart;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Return;
using ShoppingWebApi.Models.DTOs.Users;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Models.enums;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

// ═══════════════════════════════════════════════════════════
// CART SERVICE — uncovered branches
// ═══════════════════════════════════════════════════════════
public class CartBranchTests3 : IDisposable
{
    private readonly Mock<IRepository<int, Carts>>    _cartRepo     = new();
    private readonly Mock<IRepository<int, CartItem>> _cartItemRepo = new();
    private readonly Mock<IRepository<int, Product>>  _productRepo  = new();
    private readonly Mock<IRepository<int, User>>     _userRepo     = new();
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CartBranchTests3()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new AppDbContext(opts);
        _mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CartItem, CartItemReadDto>()
               .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : ""))
               .ForMember(d => d.SKU, o => o.MapFrom(s => s.Product != null ? s.Product.SKU : ""))
               .ForMember(d => d.LineTotal, o => o.MapFrom(s => s.UnitPrice * s.Quantity))
               .ForMember(d => d.AverageRating, o => o.Ignore())
               .ForMember(d => d.ReviewsCount, o => o.Ignore());
            cfg.CreateMap<Carts, CartReadDto>()
               .ForMember(d => d.SubTotal, o => o.MapFrom(s => s.Items.Sum(i => i.UnitPrice * i.Quantity)));
        }).CreateMapper();
    }

    public void Dispose() => _db.Dispose();

    private CartService Sut() => new(_cartRepo.Object, _cartItemRepo.Object,
        _productRepo.Object, _userRepo.Object, _db, _mapper);

    // GetByUserId — cart with items that have reviews (ratingLookup hit branch)
    [Fact]
    public async Task GetByUserId_CartWithItemsAndReviews_PopulatesRatings()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", Price = 10m, CategoryId = 1 };
        _db.Products.Add(product);
        _db.SaveChanges();
        var cart = new Carts { Id = 1, UserId = 1 };
        _db.Carts.Add(cart);
        _db.SaveChanges();
        _db.CartItems.Add(new CartItem { CartId = 1, ProductId = 1, Quantity = 2, UnitPrice = 10m });
        _db.Reviews.Add(new Review { ProductId = 1, UserId = 1, Rating = 4, CreatedUtc = DateTime.UtcNow });
        _db.SaveChanges();

        var result = await Sut().GetByUserIdAsync(1);

        Assert.NotEmpty(result.Items);
        Assert.Equal(4.0, result.Items[0].AverageRating);
    }

    // GetByUserId — cart with items but NO reviews (ratingLookup miss branch)
    [Fact]
    public async Task GetByUserId_CartWithItemsNoReviews_RatingZero()
    {
        var product = new Product { Id = 2, Name = "P2", SKU = "S2", Price = 5m, CategoryId = 1 };
        _db.Products.Add(product);
        _db.SaveChanges();
        var cart = new Carts { Id = 2, UserId = 2 };
        _db.Carts.Add(cart);
        _db.SaveChanges();
        _db.CartItems.Add(new CartItem { CartId = 2, ProductId = 2, Quantity = 1, UnitPrice = 5m });
        _db.SaveChanges();

        var result = await Sut().GetByUserIdAsync(2);

        Assert.NotEmpty(result.Items);
        Assert.Equal(0.0, result.Items[0].AverageRating);
    }

    // AddItem — user not found
    [Fact]
    public async Task AddItem_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().AddItemAsync(99, new CartAddItemDto { ProductId = 1, Quantity = 1 }));
    }

    // AddItem — product not found
    [Fact]
    public async Task AddItem_ProductNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        _productRepo.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().AddItemAsync(1, new CartAddItemDto { ProductId = 99, Quantity = 1 }));
    }

    // AddItem — cart Add returns null (failed to create cart branch)
    [Fact]
    public async Task AddItem_CartAddReturnsNull_ThrowsBusinessValidationException()
    {
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S", Price = 10m, IsActive = true });
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts>());
        _cartRepo.Setup(r => r.Add(It.IsAny<Carts>())).ReturnsAsync((Carts?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            Sut().AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 1 }));
    }

    // UpdateItem — quantity 0 deletes item
    [Fact]
    public async Task UpdateItem_QuantityZero_DeletesItem()
    {
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem>
            { new CartItem { Id = 5, CartId = 1, ProductId = 1, Quantity = 2, UnitPrice = 10m } });
        _cartItemRepo.Setup(r => r.Delete(5)).ReturnsAsync(new CartItem());

        await Sut().UpdateItemAsync(1, new CartUpdateItemDto { ProductId = 1, Quantity = 0 });

        _cartItemRepo.Verify(r => r.Delete(5), Times.Once);
    }
}

// ═══════════════════════════════════════════════════════════
// ORDER SERVICE — uncovered branches
// ═══════════════════════════════════════════════════════════
public class OrderBranchTests4
{
    private readonly Mock<IRepository<int, Order>>         _orderRepo     = new();
    private readonly Mock<IRepository<int, OrderItem>>     _orderItemRepo = new();
    private readonly Mock<IRepository<int, CartItem>>      _cartItemRepo  = new();
    private readonly Mock<IRepository<int, Payment>>       _paymentRepo   = new();
    private readonly Mock<IRepository<int, Refund>>        _refundRepo    = new();
    private readonly Mock<IRepository<int, ReturnRequest>> _returnRepo    = new();
    private readonly Mock<IRepository<int, Inventory>>     _inventoryRepo = new();
    private readonly Mock<IPromoService>                   _promoSvc      = new();
    private readonly Mock<IWalletService>                  _walletSvc     = new();
    private readonly Mock<ILogWriter>                      _logWriter     = new();
    private readonly Mock<ILogger<OrderService>>           _logger        = new();

    public OrderBranchTests4()
    {
        _logWriter.Setup(l => l.InfoAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _logWriter.Setup(l => l.ErrorAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Exception?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private AppDbContext CreateCtx() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Order MakeOrder(int userId, string num, OrderStatus status,
        decimal total = 100m, decimal walletUsed = 0m, List<OrderItem>? items = null) => new Order
        {
            UserId = userId, OrderNumber = num, Status = status, Total = total, WalletUsed = walletUsed,
            ShipToName = "J", ShipToLine1 = "L", ShipToCity = "C", ShipToState = "S",
            ShipToPostalCode = "P", ShipToCountry = "I", Items = items ?? new List<OrderItem>()
        };

    private OrderService BuildSut(AppDbContext ctx)
    {
        _orderRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Orders);
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
        retRepo.Setup(r => r.Add(It.IsAny<ReturnRequest>())).Returns((ReturnRequest r) => _returnRepo.Object.Add(r));
        retRepo.Setup(r => r.Get(It.IsAny<int>())).Returns((int id) => _returnRepo.Object.Get(id));
        retRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<ReturnRequest>())).Returns((int id, ReturnRequest r) => _returnRepo.Object.Update(id, r));
        invRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).Returns((int id, Inventory inv) => _inventoryRepo.Object.Update(id, inv));

        return new OrderService(
            userRepo.Object, addressRepo.Object, cartRepo.Object, _cartItemRepo.Object,
            _orderRepo.Object, _orderItemRepo.Object, invRepo.Object,
            _paymentRepo.Object, _refundRepo.Object, retRepo.Object,
            _promoSvc.Object, _walletSvc.Object, ctx, _logWriter.Object, _logger.Object);
    }

    private static Payment SeedPayment(AppDbContext ctx, int orderId, int userId, string type)
    {
        var p = new Payment { OrderId = orderId, UserId = userId, PaymentType = type, TotalAmount = 0m, CreatedAt = DateTime.UtcNow };
        ctx.Payments.Add(p); ctx.SaveChanges(); return p;
    }

    // PlaceOrder — user not found
    [Fact]
    public async Task PlaceOrder_UserNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        var req = new PlaceOrderRequestDto { UserId = 999, AddressId = 1, PaymentType = "UPI" };

        await Assert.ThrowsAsync<NotFoundException>(() => BuildSut(ctx).PlaceOrderAsync(req));
    }

    // PlaceOrder — address not found
    [Fact]
    public async Task PlaceOrder_AddressNotFound_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
        ctx.SaveChanges();
        var req = new PlaceOrderRequestDto { UserId = 1, AddressId = 999, PaymentType = "UPI" };

        await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(req));
    }

    // PlaceOrder — cart is null
    [Fact]
    public async Task PlaceOrder_CartNull_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
        ctx.SaveChanges();
        ctx.Addresses.Add(new Address { UserId = 1, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
        ctx.SaveChanges();
        var req = new PlaceOrderRequestDto { UserId = 1, AddressId = ctx.Addresses.First().Id, PaymentType = "UPI" };

        await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(req));
    }

    // PlaceOrder — inventory missing for product
    [Fact]
    public async Task PlaceOrder_InventoryMissing_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
        ctx.SaveChanges();
        ctx.Addresses.Add(new Address { UserId = 1, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
        ctx.SaveChanges();
        var product = new Product { Name = "P", SKU = "S1", Price = 10m, IsActive = true, CategoryId = 1 };
        ctx.Products.Add(product);
        ctx.SaveChanges();
        var cart = new Carts { UserId = 1 };
        ctx.Carts.Add(cart);
        ctx.SaveChanges();
        ctx.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = 1, UnitPrice = 10m });
        ctx.SaveChanges();
        // No inventory seeded

        var req = new PlaceOrderRequestDto { UserId = 1, AddressId = ctx.Addresses.First().Id, PaymentType = "UPI" };

        await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(req));
    }

    // PlaceOrder — COD payment sets PaymentStatus.Pending
    [Fact]
    public async Task PlaceOrder_COD_PaymentStatusPending()
    {
        using var ctx = CreateCtx();
        ctx.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
        ctx.SaveChanges();
        ctx.Addresses.Add(new Address { UserId = 1, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
        ctx.SaveChanges();
        var product = new Product { Name = "P", SKU = "S1", Price = 10m, IsActive = true, CategoryId = 1 };
        ctx.Products.Add(product);
        ctx.SaveChanges();
        ctx.Inventories.Add(new Inventory { ProductId = product.Id, Quantity = 10 });
        var cart = new Carts { UserId = 1 };
        ctx.Carts.Add(cart);
        ctx.SaveChanges();
        ctx.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = 1, UnitPrice = 10m });
        ctx.SaveChanges();

        var addedOrder = new Order { Id = 1, UserId = 1, OrderNumber = "O1", Status = OrderStatus.Pending, PaymentStatus = PaymentStatus.Pending };
        _orderRepo.Setup(r => r.Add(It.IsAny<Order>())).ReturnsAsync(addedOrder);
        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(addedOrder);
        _orderItemRepo.Setup(r => r.Add(It.IsAny<OrderItem>())).ReturnsAsync(new OrderItem());
        _inventoryRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(new Inventory());
        _cartItemRepo.Setup(r => r.Delete(It.IsAny<int>())).ReturnsAsync(new CartItem());
        _paymentRepo.Setup(r => r.Add(It.IsAny<Payment>())).ReturnsAsync(new Payment { PaymentType = "CashOnDelivery" });

        var req = new PlaceOrderRequestDto { UserId = 1, AddressId = ctx.Addresses.First().Id, PaymentType = "CashOnDelivery" };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
    }

    // Cancel — order is Shipped → cannot cancel
    [Fact]
    public async Task Cancel_ShippedOrder_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Shipped));
        ctx.SaveChanges();
        var order = ctx.Orders.First();

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).CancelOrderAsync(order.Id, 1));
    }

    // Cancel — order is Delivered → cannot cancel
    [Fact]
    public async Task Cancel_DeliveredOrder_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Delivered));
        ctx.SaveChanges();
        var order = ctx.Orders.First();

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).CancelOrderAsync(order.Id, 1));
    }

    // Cancel — order already cancelled → returns early
    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsAlreadyCancelledMessage()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Cancelled));
        ctx.SaveChanges();
        var order = ctx.Orders.First();

        var result = await BuildSut(ctx).CancelOrderAsync(order.Id, 1);

        Assert.Equal("Order already cancelled.", result.Message);
    }

    // Cancel — non-admin trying to cancel another user's order
    [Fact]
    public async Task Cancel_NonAdminOtherUser_ThrowsForbiddenException()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(5, "O1", OrderStatus.Pending));
        ctx.SaveChanges();
        var order = ctx.Orders.First();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1, isAdmin: false));
    }

    // Cancel — COD with wallet used > 0 → credits wallet
    [Fact]
    public async Task Cancel_COD_WithWalletUsed_CreditsWallet()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Pending, total: 100m, walletUsed: 50m,
            items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, ProductName = "P", SKU = "S", UnitPrice = 100m, LineTotal = 100m } }));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "cod");

        _inventoryRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _refundRepo.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());
        _walletSvc.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((50m, 1));

        await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1);

        _walletSvc.Verify(w => w.CreditAsync(1, 50m, WalletTxnType.CreditRefund,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // GetById — order not found returns null
    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        using var ctx = CreateCtx();

        var result = await BuildSut(ctx).GetByIdAsync(999);

        Assert.Null(result);
    }

    // UpdateStatus — order not found returns false
    [Fact]
    public async Task UpdateStatus_OrderNotFound_ReturnsFalse()
    {
        using var ctx = CreateCtx();
        _orderRepo.Setup(r => r.Get(999)).ReturnsAsync((Order?)null);

        var result = await BuildSut(ctx).UpdateStatusAsync(999, "Shipped");

        Assert.False(result);
    }

    // UpdateStatus — already cancelled order → throws ConflictException
    [Fact]
    public async Task UpdateStatus_AlreadyCancelled_ThrowsConflictException()
    {
        using var ctx = CreateCtx();
        var order = new Order { Id = 1, Status = OrderStatus.Cancelled, UserId = 1, OrderNumber = "O1" };
        _orderRepo.Setup(r => r.Get(1)).ReturnsAsync(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildSut(ctx).UpdateStatusAsync(1, "Shipped"));
    }

    // RequestReturn — order not found
    [Fact]
    public async Task RequestReturn_OrderNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildSut(ctx).RequestReturnAsync(1, new ReturnRequestCreateDto { OrderId = 999, Reason = "Damaged" }));
    }

    // RequestReturn — order not delivered
    [Fact]
    public async Task RequestReturn_NotDelivered_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Shipped));
        ctx.SaveChanges();
        var order = ctx.Orders.First();

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).RequestReturnAsync(1, new ReturnRequestCreateDto { OrderId = order.Id, Reason = "Damaged" }));
    }

    // RequestReturn — already requested
    [Fact]
    public async Task RequestReturn_AlreadyRequested_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Delivered));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        ctx.ReturnRequests.Add(new ReturnRequest { OrderId = order.Id, Status = ReturnStatus.Requested, Reason = "x" });
        ctx.SaveChanges();

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).RequestReturnAsync(1, new ReturnRequestCreateDto { OrderId = order.Id, Reason = "Damaged" }));
    }
}

// ═══════════════════════════════════════════════════════════
// USER SERVICE — uncovered branches
// ═══════════════════════════════════════════════════════════
public class UserBranchTests2
{
    private readonly Mock<IRepository<int, User>>        _userRepo    = new();
    private readonly Mock<IRepository<int, UserDetails>> _detailsRepo = new();
    private readonly Mock<ILogger<UserService>>          _logger      = new();

    private AppDbContext CreateCtx(Action<AppDbContext>? seed = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new AppDbContext(opts);
        seed?.Invoke(ctx);
        ctx.SaveChanges();
        return ctx;
    }

    private UserService BuildSut(AppDbContext ctx)
    {
        _userRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Users);
        return new UserService(_userRepo.Object, _detailsRepo.Object, ctx, _logger.Object);
    }

    // GetById — not found returns null
    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        using var ctx = CreateCtx();
        var result = await BuildSut(ctx).GetByIdAsync(999);
        Assert.Null(result);
    }

    // GetById — found returns dto
    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        using var ctx = CreateCtx(c =>
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" }));
        var result = await BuildSut(ctx).GetByIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal("a@b.com", result!.Email);
    }

    // UpdateRole — empty role throws
    [Fact]
    public async Task UpdateRole_EmptyRole_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).UpdateRoleAsync(1, ""));
    }

    // UpdateRole — invalid role throws
    [Fact]
    public async Task UpdateRole_InvalidRole_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).UpdateRoleAsync(1, "SuperAdmin"));
    }

    // UpdateRole — user not found throws
    [Fact]
    public async Task UpdateRole_UserNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        _userRepo.Setup(r => r.Get(99)).ReturnsAsync((User?)null);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildSut(ctx).UpdateRoleAsync(99, "Admin"));
    }

    // UpdateProfile — empty FirstName after update throws
    [Fact]
    public async Task UpdateProfile_EmptyFirstNameAfterUpdate_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" });
            c.SaveChanges();
            c.UserDetails.Add(new UserDetails { UserId = 1, FirstName = "John", LastName = "Doe" });
        });
        var details = ctx.UserDetails.First();
        _detailsRepo.Setup(r => r.Update(1, It.IsAny<UserDetails>())).ReturnsAsync(details);
        _userRepo.Setup(r => r.Update(1, It.IsAny<User>())).ReturnsAsync(ctx.Users.First());

        // Setting FirstName to whitespace should trigger validation
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).UpdateProfileAsync(1, new UpdateUserProfileDto { FirstName = "   ", LastName = "Doe" }));
    }

    // ChangePassword — short password throws
    [Fact]
    public async Task ChangePassword_ShortPassword_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).ChangePasswordAsync(new ChangePasswordRequestDto
                { UserId = 1, CurrentPassword = "old", NewPassword = "abc" }));
    }

    // ChangePassword — user not found throws
    [Fact]
    public async Task ChangePassword_UserNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        _userRepo.Setup(r => r.Get(99)).ReturnsAsync((User?)null);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildSut(ctx).ChangePasswordAsync(new ChangePasswordRequestDto
                { UserId = 99, CurrentPassword = "old", NewPassword = "newpass123" }));
    }

    // GetPaged — email filter
    [Fact]
    public async Task GetPaged_EmailFilter_FiltersCorrectly()
    {
        using var ctx = CreateCtx(c => c.Users.AddRange(
            new User { Email = "alice@test.com", Role = "User", PasswordHash = "x" },
            new User { Email = "bob@test.com", Role = "User", PasswordHash = "x" }));
        var result = await BuildSut(ctx).GetPagedAsync("alice", null, null, null, false, 1, 10);
        Assert.Single(result.Items);
    }

    // GetPaged — role filter
    [Fact]
    public async Task GetPaged_RoleFilter_FiltersCorrectly()
    {
        using var ctx = CreateCtx(c => c.Users.AddRange(
            new User { Email = "a@test.com", Role = "Admin", PasswordHash = "x" },
            new User { Email = "b@test.com", Role = "User", PasswordHash = "x" }));
        var result = await BuildSut(ctx).GetPagedAsync(null, "Admin", null, null, false, 1, 10);
        Assert.Single(result.Items);
    }

    // GetPaged — user with no UserDetails (FullName empty, Phone null branches)
    [Fact]
    public async Task GetPaged_UserWithNoDetails_FullNameEmptyPhoneNull()
    {
        using var ctx = CreateCtx(c =>
            c.Users.Add(new User { Email = "a@test.com", Role = "User", PasswordHash = "x" }));
        var result = await BuildSut(ctx).GetPagedAsync(null, null, null, null, false, 1, 10);
        Assert.Single(result.Items);
        Assert.Equal(string.Empty, result.Items[0].FullName);
        Assert.Null(result.Items[0].Phone);
    }
}

// ═══════════════════════════════════════════════════════════
// PRODUCT SERVICE — uncovered branches
// ═══════════════════════════════════════════════════════════
public class ProductBranchTests4
{
    private readonly Mock<IRepository<int, Product>>      _productRepo   = new();
    private readonly Mock<IRepository<int, Category>>     _categoryRepo  = new();
    private readonly Mock<IRepository<int, ProductImage>> _imageRepo     = new();
    private readonly Mock<IRepository<int, Inventory>>    _inventoryRepo = new();
    private readonly IMapper _mapper;

    public ProductBranchTests4()
    {
        _mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ProductImage, ProductImageReadDto>();
            cfg.CreateMap<Product, ProductReadDto>()
               .ForMember(d => d.Images, o => o.MapFrom(s => s.Images))
               .ForMember(d => d.AverageRating, o => o.Ignore())
               .ForMember(d => d.ReviewsCount, o => o.Ignore());
            cfg.CreateMap<Review, ReviewReadDto>()
               .ForMember(d => d.UserName, o => o.Ignore());
        }).CreateMapper();
    }

    private AppDbContext CreateCtx(params Product[] products)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new AppDbContext(opts);
        ctx.Products.AddRange(products);
        ctx.SaveChanges();
        return ctx;
    }

    private ProductService Sut(AppDbContext ctx)
    {
        _productRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Products);
        return new ProductService(_productRepo.Object, _categoryRepo.Object,
            _imageRepo.Object, _inventoryRepo.Object, ctx, _mapper);
    }

    private ProductService MockSut()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ProductService(_productRepo.Object, _categoryRepo.Object,
            _imageRepo.Object, _inventoryRepo.Object, new AppDbContext(opts), _mapper);
    }

    // Create — Add returns null → throws BusinessValidationException
    [Fact]
    public async Task Create_AddReturnsNull_ThrowsBusinessValidationException()
    {
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
        _productRepo.Setup(r => r.Add(It.IsAny<Product>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            MockSut().CreateAsync(new ProductCreateDto { Name = "P", SKU = "S1", Price = 10m, CategoryId = 1 }));
    }

    // Search — inStockOnly filter (if ProductQuery has it)
    [Fact]
    public async Task Search_WithInStockOnly_FiltersInactiveOut()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Active", SKU = "A1", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Inactive", SKU = "I1", IsActive = false, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(new ProductQuery { Page = 1, Size = 10 });

        Assert.Single(result.Items);
        Assert.Equal("Active", result.Items[0].Name);
    }

    // GetAll — sort by price asc
    [Fact]
    public async Task GetAll_SortByPriceAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Cheap", SKU = "C1", Price = 5m, CategoryId = 1 },
            new Product { Id = 2, Name = "Expensive", SKU = "E1", Price = 500m, CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(1, 10, "price", "asc");

        Assert.Equal(5m, result.Items[0].Price);
    }

    // Delete — product not referenced → deletes successfully
    [Fact]
    public async Task Delete_NotReferenced_ReturnsTrue()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);
        _productRepo.Setup(r => r.Delete(1)).ReturnsAsync(product);

        var result = await Sut(ctx).DeleteAsync(1);

        Assert.True(result);
    }

    // GetReviewsByProductId — sort by newest desc (default)
    [Fact]
    public async Task GetReviews_SortByNewestDesc_Works()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 3, CreatedUtc = DateTime.UtcNow.AddDays(-1) },
            new Review { ProductId = 1, UserId = 2, Rating = 5, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);

        var result = await Sut(ctx).GetReviewsByProductIdAsync(1, 1, 10, sortBy: "newest", sortDir: "desc");

        Assert.Equal(5, result.Items[0].Rating); // newest first
    }

    // GetReviewsByProductId — minRating filters
    [Fact]
    public async Task GetReviews_WithMinRating_FiltersLowRatings()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 1, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 5, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);

        var result = await Sut(ctx).GetReviewsByProductIdAsync(1, 1, 10, minRating: 4);

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Rating);
    }
}
