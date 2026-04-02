using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Auth;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Repositories;
using ShoppingWebApi.Services;
using ShoppingWebApi.Services.Security;
using System.Security.Claims;

namespace ShoppingApp.Tests.Services;

// ═══════════════════════════════════════════════════════════
// AUTH SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class AuthBranchTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IRepository<int, User> _userRepo;
    private readonly IRepository<int, UserDetails> _detailsRepo;
    private readonly Mock<ITokenService> _tokenMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly AuthService _sut;

    public AuthBranchTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new AppDbContext(opts);
        _userRepo    = new Repository<int, User>(_db);
        _detailsRepo = new Repository<int, UserDetails>(_db);

        _tokenMock.Setup(t => t.CreateAccessToken(
            It.IsAny<IEnumerable<Claim>>(), out It.Ref<DateTime>.IsAny))
            .Returns((IEnumerable<Claim> _, out DateTime exp) => { exp = DateTime.UtcNow.AddHours(1); return "tok"; });

        _sut = new AuthService(_userRepo, _detailsRepo, _tokenMock.Object, _loggerMock.Object);
    }

    public void Dispose() => _db.Dispose();

    // Register — only LastName provided → creates UserDetails
    [Fact]
    public async Task Register_OnlyLastName_CreatesUserDetails()
    {
        var dto = new RegisterRequestDto { Email = "ln@test.com", Password = "Pass@1", LastName = "Smith" };
        await _sut.RegisterAsync(dto);
        var user    = _db.Users.First(u => u.Email == dto.Email);
        var details = _db.UserDetails.FirstOrDefault(d => d.UserId == user.Id);
        Assert.NotNull(details);
        Assert.Equal("Smith", details!.LastName);
    }

    // Register — only Phone provided → creates UserDetails
    [Fact]
    public async Task Register_OnlyPhone_CreatesUserDetails()
    {
        var dto = new RegisterRequestDto { Email = "ph@test.com", Password = "Pass@1", Phone = "9999999999" };
        await _sut.RegisterAsync(dto);
        var user    = _db.Users.First(u => u.Email == dto.Email);
        var details = _db.UserDetails.FirstOrDefault(d => d.UserId == user.Id);
        Assert.NotNull(details);
        Assert.Equal("9999999999", details!.Phone);
    }

    // Register — Role is null → defaults to "User" (BuildClaims uses Role ?? "User")
    [Fact]
    public async Task Register_NullRole_BuildsClaimsWithUserRole()
    {
        var dto = new RegisterRequestDto { Email = "nr@test.com", Password = "Pass@1", Role = null };
        var result = await _sut.RegisterAsync(dto);
        Assert.NotNull(result.AccessToken);
        var user = _db.Users.First(u => u.Email == dto.Email);
        Assert.Equal("User", user.Role);
    }

    // Login — user found, correct password → success
    [Fact]
    public async Task Login_CorrectPassword_ReturnsToken()
    {
        var hash = PasswordHasher.Hash("correct");
        _db.Users.Add(new User { Email = "ok@test.com", PasswordHash = hash, Role = "User" });
        await _db.SaveChangesAsync();

        var result = await _sut.LoginAsync(new LoginRequestDto { Email = "ok@test.com", Password = "correct" });
        Assert.Equal("tok", result.AccessToken);
    }

    // Login — user Role is null → BuildClaims uses "User"
    [Fact]
    public async Task Login_NullRole_BuildsClaimsWithUserRole()
    {
        var hash = PasswordHasher.Hash("pass");
        _db.Users.Add(new User { Email = "nullrole@test.com", PasswordHash = hash, Role = null });
        await _db.SaveChangesAsync();

        var result = await _sut.LoginAsync(new LoginRequestDto { Email = "nullrole@test.com", Password = "pass" });
        Assert.NotNull(result.AccessToken);
    }
}

// ═══════════════════════════════════════════════════════════
// WISHLIST SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class WishlistBranchTests2
{
    private readonly Mock<IRepository<int, WishlistItem>> _wishlistRepo = new();
    private readonly Mock<IRepository<int, Product>>      _productRepo  = new();
    private readonly Mock<IRepository<int, Carts>>        _cartRepo     = new();
    private readonly Mock<IRepository<int, CartItem>>     _cartItemRepo = new();

    private AppDbContext CreateCtx(Action<AppDbContext>? seed = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new AppDbContext(opts);
        seed?.Invoke(ctx);
        ctx.SaveChanges();
        return ctx;
    }

    private WishlistService Sut(AppDbContext ctx)
    {
        _wishlistRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.WishlistItems);
        _cartRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Carts);
        return new WishlistService(_wishlistRepo.Object, _productRepo.Object,
            _cartRepo.Object, _cartItemRepo.Object);
    }

    // MoveToCart — wishlist item NOT found after move (wishItem == null branch)
    [Fact]
    public async Task MoveToCart_WishlistItemNotFound_StillReturnsTrue()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
            c.SaveChanges();
            c.Carts.Add(new Carts { Id = 1, UserId = 1 });
            // No wishlist item seeded — wishItem will be null
        });

        _productRepo.Setup(r => r.Get(5)).ReturnsAsync(new Product { Id = 5, Name = "P", SKU = "S", Price = 10m });
        _cartItemRepo.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

        var result = await Sut(ctx).MoveToCartAsync(1, 5);

        Assert.True(result);
        // wishlistRepo.Delete should NOT be called since no wishlist item exists
        _wishlistRepo.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }

    // MoveToCart — wishlist item found → deleted after move
    [Fact]
    public async Task MoveToCart_WishlistItemFound_DeletesFromWishlist()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
            c.SaveChanges();
            c.Carts.Add(new Carts { Id = 1, UserId = 1 });
            c.SaveChanges();
            c.WishlistItems.Add(new WishlistItem { Id = 1, UserId = 1, ProductId = 5 });
        });

        _productRepo.Setup(r => r.Get(5)).ReturnsAsync(new Product { Id = 5, Name = "P", SKU = "S", Price = 10m });
        _cartItemRepo.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());
        _wishlistRepo.Setup(r => r.Delete(1)).ReturnsAsync(new WishlistItem());

        await Sut(ctx).MoveToCartAsync(1, 5);

        _wishlistRepo.Verify(r => r.Delete(1), Times.Once);
    }

    // GetAsync — empty wishlist returns empty list
    [Fact]
    public async Task GetAsync_EmptyWishlist_ReturnsEmpty()
    {
        using var ctx = CreateCtx();
        var result = await Sut(ctx).GetAsync(99);
        Assert.Empty(result);
    }
}

// ═══════════════════════════════════════════════════════════
// PRODUCT SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class ProductBranchTests
{
    private readonly Mock<IRepository<int, Product>>      _productRepo   = new();
    private readonly Mock<IRepository<int, Category>>     _categoryRepo  = new();
    private readonly Mock<IRepository<int, ProductImage>> _imageRepo     = new();
    private readonly Mock<IRepository<int, Inventory>>    _inventoryRepo = new();
    private readonly IMapper _mapper;

    public ProductBranchTests()
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
        var db = new AppDbContext(opts);
        return new(_productRepo.Object, _categoryRepo.Object, _imageRepo.Object, _inventoryRepo.Object, db, _mapper);
    }

    // Update — GetAll returns null (null-coalescing branch)
    [Fact]
    public async Task Update_GetAllReturnsNull_TreatsAsEmpty()
    {
        var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1, Reviews = new List<Review>() };
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Product>?)null);
        _productRepo.Setup(r => r.Update(1, It.IsAny<Product>())).ReturnsAsync(existing);

        using var ctx = CreateCtx(existing);
        var result = await Sut(ctx).UpdateAsync(1,
            new ProductUpdateDto { Id = 1, Name = "B", SKU = "S1", Price = 5m, CategoryId = 1 });

        Assert.NotNull(result);
    }

    // Search — no filters, returns all active
    [Fact]
    public async Task Search_NoFilters_ReturnsAllActive()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", IsActive = true, CategoryId = 1 },
            new Product { Id = 3, Name = "P3", SKU = "S3", IsActive = false, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(new ProductQuery { Page = 1, Size = 10 });

        Assert.Equal(2, result.TotalCount);
    }

    // Search — PriceMax only
    [Fact]
    public async Task Search_PriceMaxOnly_FiltersCorrectly()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Cheap", SKU = "C1", Price = 10m, IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Expensive", SKU = "E1", Price = 1000m, IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(new ProductQuery { PriceMax = 100m, Page = 1, Size = 10 });

        Assert.Single(result.Items);
    }

    // GetAll — page/size zero defaults
    [Fact]
    public async Task GetAll_ZeroPage_DefaultsToOne()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(0, 0);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
    }

    // GetAll — sort by name asc (already covered but ensure desc also)
    [Fact]
    public async Task GetAll_SortByPriceAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", Price = 100m, CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", Price = 10m, CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(1, 10, "price", "asc");

        Assert.Equal(10m, result.Items[0].Price);
    }
}

// ═══════════════════════════════════════════════════════════
// ORDER SERVICE — branch coverage (remaining)
// ═══════════════════════════════════════════════════════════
public class OrderBranchTests
{
    private readonly Mock<IRepository<int, Order>>         _orderRepo       = new();
    private readonly Mock<IRepository<int, OrderItem>>     _orderItemRepo   = new();
    private readonly Mock<IRepository<int, CartItem>>      _cartItemRepo    = new();
    private readonly Mock<IRepository<int, Payment>>       _paymentRepo     = new();
    private readonly Mock<IRepository<int, Refund>>        _refundRepo      = new();
    private readonly Mock<IRepository<int, ReturnRequest>> _returnRepo      = new();
    private readonly Mock<IRepository<int, Inventory>>     _inventoryRepo   = new();
    private readonly Mock<IPromoService>                   _promoSvc        = new();
    private readonly Mock<IWalletService>                  _walletSvc       = new();
    private readonly Mock<ILogWriter>                      _logWriter       = new();
    private readonly Mock<ILogger<OrderService>>           _logger          = new();

    public OrderBranchTests()
    {
        _logWriter.Setup(l => l.InfoAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _logWriter.Setup(l => l.ErrorAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Exception?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private AppDbContext CreateCtx()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private static Order MakeOrder(int userId, string num, ShoppingWebApi.Models.enums.OrderStatus status,
        decimal total = 100m, decimal walletUsed = 0m, List<OrderItem>? items = null) => new Order
        {
            UserId = userId, OrderNumber = num, Status = status, Total = total, WalletUsed = walletUsed,
            ShipToName = "J", ShipToLine1 = "L", ShipToCity = "C", ShipToState = "S",
            ShipToPostalCode = "P", ShipToCountry = "I", Items = items ?? new List<OrderItem>()
        };

    private static Payment SeedPayment(AppDbContext ctx, int orderId, int userId, string type)
    {
        var p = new Payment { OrderId = orderId, UserId = userId, PaymentType = type, TotalAmount = 0m, CreatedAt = DateTime.UtcNow };
        ctx.Payments.Add(p); ctx.SaveChanges(); return p;
    }

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

    // Cancel — admin can cancel another user's order
    [Fact]
    public async Task Cancel_AdminCancelsOtherUserOrder_Success()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(5, "O1", ShoppingWebApi.Models.enums.OrderStatus.Pending, total: 100m,
            items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, ProductName = "P", SKU = "S", UnitPrice = 100m, LineTotal = 100m } }));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "UPI");

        _inventoryRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _refundRepo.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());
        _walletSvc.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<ShoppingWebApi.Models.enums.WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((100m, 1));

        // Admin (userId=1) cancels order belonging to userId=5
        var result = await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1, isAdmin: true);

        Assert.Equal("Cancelled", result.Status);
    }

    // Cancel — order has no payment (Payment == null branch)
    [Fact]
    public async Task Cancel_NoPayment_SkipsRefundRow()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(1, "O1", ShoppingWebApi.Models.enums.OrderStatus.Pending, total: 0m));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        // No payment seeded

        _inventoryRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);

        var result = await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1);

        Assert.Equal("Cancelled", result.Status);
        _refundRepo.Verify(r => r.Add(It.IsAny<Refund>()), Times.Never);
    }

    // Cancel — COD with no wallet used (walletUsed == 0 branch)
    [Fact]
    public async Task Cancel_COD_NoWalletUsed_NoWalletCredit()
    {
        using var ctx = CreateCtx();
        ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
        ctx.SaveChanges();
        var inv = ctx.Inventories.First();

        ctx.Orders.Add(MakeOrder(1, "O1", ShoppingWebApi.Models.enums.OrderStatus.Pending, total: 100m, walletUsed: 0m,
            items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, ProductName = "P", SKU = "S", UnitPrice = 100m, LineTotal = 100m } }));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "cod");

        _inventoryRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _refundRepo.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());

        await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1);

        // No wallet credit since walletUsed == 0
        _walletSvc.Verify(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<ShoppingWebApi.Models.enums.WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Cancel — online payment with total == 0 (no wallet credit branch)
    [Fact]
    public async Task Cancel_OnlinePayment_ZeroTotal_NoWalletCredit()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", ShoppingWebApi.Models.enums.OrderStatus.Pending, total: 0m));
        ctx.SaveChanges();
        var order = ctx.Orders.First();
        SeedPayment(ctx, order.Id, order.UserId, "UPI");

        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
        _refundRepo.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());

        await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1);

        _walletSvc.Verify(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<ShoppingWebApi.Models.enums.WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // GetUserOrders — invalid status string (Enum.TryParse fails → no filter applied)
    [Fact]
    public async Task GetUserOrders_InvalidStatus_ReturnsAllOrders()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", ShoppingWebApi.Models.enums.OrderStatus.Pending),
            MakeOrder(1, "O2", ShoppingWebApi.Models.enums.OrderStatus.Delivered));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10, status: "NotAStatus");

        Assert.Equal(2, result.TotalCount);
    }

    // GetAll — invalid status string (Enum.TryParse fails → no filter)
    [Fact]
    public async Task GetAll_InvalidStatus_ReturnsAll()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            MakeOrder(1, "O1", ShoppingWebApi.Models.enums.OrderStatus.Pending),
            MakeOrder(2, "O2", ShoppingWebApi.Models.enums.OrderStatus.Shipped));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(status: "BadStatus", page: 1, size: 10);

        Assert.Equal(2, result.TotalCount);
    }
}
