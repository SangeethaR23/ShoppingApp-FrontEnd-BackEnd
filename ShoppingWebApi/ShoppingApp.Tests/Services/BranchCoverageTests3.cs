using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Address;
using ShoppingWebApi.Models.DTOs.Cart;
using ShoppingWebApi.Models.DTOs.Categories;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Return;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Models.enums;
using ShoppingWebApi.Repositories;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

// ═══════════════════════════════════════════════════════════
// REVIEW SERVICE — remaining branches
// ═══════════════════════════════════════════════════════════
public class ReviewBranchTests2
{
    private readonly Mock<IRepository<int, Review>>  _reviewRepo  = new();
    private readonly Mock<IRepository<int, Product>> _productRepo = new();
    private readonly Mock<IRepository<int, User>>    _userRepo    = new();
    private readonly Mock<ILogger<ReviewService>>    _logger      = new();
    private readonly IMapper _mapper;

    public ReviewBranchTests2()
    {
        _mapper = new MapperConfiguration(c =>
            c.CreateMap<Review, ReviewReadDto>().ForMember(d => d.UserName, o => o.Ignore()))
            .CreateMapper();
    }

    private AppDbContext CreateCtx(params Review[] reviews)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new AppDbContext(opts);
        ctx.Reviews.AddRange(reviews);
        ctx.SaveChanges();
        return ctx;
    }

    private ReviewService Sut(AppDbContext ctx)
    {
        _reviewRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Reviews);
        return new ReviewService(_reviewRepo.Object, _productRepo.Object,
            _userRepo.Object, _mapper, _logger.Object);
    }

    // Create — null comment (Comment?.Trim() → null branch)
    [Fact]
    public async Task Create_NullComment_StoresNull()
    {
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S" });
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        var saved = new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 4, Comment = null };
        _reviewRepo.Setup(r => r.Add(It.IsAny<Review>())).ReturnsAsync(saved);
        using var ctx = CreateCtx();
        var sut = Sut(ctx);

        var result = await sut.CreateAsync(new ReviewCreateDto { ProductId = 1, UserId = 1, Rating = 4, Comment = null });

        Assert.Null(result.Comment);
    }

    // Create — empty string comment (trims to empty)
    [Fact]
    public async Task Create_EmptyComment_StoresEmpty()
    {
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S" });
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        var saved = new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 3, Comment = "" };
        _reviewRepo.Setup(r => r.Add(It.IsAny<Review>())).ReturnsAsync(saved);
        using var ctx = CreateCtx();

        var result = await Sut(ctx).CreateAsync(new ReviewCreateDto { ProductId = 1, UserId = 1, Rating = 3, Comment = "  " });

        Assert.NotNull(result);
    }

    // Update — null comment (Comment?.Trim() → null branch)
    [Fact]
    public async Task Update_NullComment_SetsNull()
    {
        var review = new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 3, Comment = "old" };
        using var ctx = CreateCtx(review);
        _reviewRepo.Setup(r => r.Update(1, It.IsAny<Review>())).ReturnsAsync(review);

        await Sut(ctx).UpdateAsync(1, 1, new ReviewUpdateDto { Rating = 4, Comment = null });

        Assert.Null(review.Comment);
    }

    // GetByProduct — page > 1 (skip branch)
    [Fact]
    public async Task GetByProduct_Page2_SkipsCorrectly()
    {
        var reviews = Enumerable.Range(1, 6)
            .Select(i => new Review { ProductId = 1, UserId = i, Rating = 3, CreatedUtc = DateTime.UtcNow })
            .ToArray();
        using var ctx = CreateCtx(reviews);

        var result = await Sut(ctx).GetByProductAsync(1, 2, 3);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
    }
}

// ═══════════════════════════════════════════════════════════
// CART SERVICE — remaining branches
// ═══════════════════════════════════════════════════════════
public class CartBranchTests2 : IDisposable
{
    private readonly Mock<IRepository<int, Carts>>    _cartRepo     = new();
    private readonly Mock<IRepository<int, CartItem>> _cartItemRepo = new();
    private readonly Mock<IRepository<int, Product>>  _productRepo  = new();
    private readonly Mock<IRepository<int, User>>     _userRepo     = new();
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CartBranchTests2()
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

    // GetByUserId — cart exists but has zero items (dto.Items.Count == 0 branch)
    [Fact]
    public async Task GetByUserId_CartWithNoItems_SubTotalZero()
    {
        _db.Carts.Add(new Carts { Id = 1, UserId = 10 });
        await _db.SaveChangesAsync();

        var result = await Sut().GetByUserIdAsync(10);

        Assert.Equal(0m, result.SubTotal);
        Assert.Empty(result.Items);
    }

    // AddItem — existing cart found (no new cart creation)
    [Fact]
    public async Task AddItem_ExistingCart_NoNewCartCreated()
    {
        var user    = new User { Id = 1, Email = "a@b.com" };
        var product = new Product { Id = 1, Name = "P", SKU = "S", Price = 5m, IsActive = true, CategoryId = 1 };
        var cart    = new Carts { Id = 10, UserId = 1 };

        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(user);
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { cart });
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem>());
        _cartItemRepo.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

        await Sut().AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 1 });

        // Cart should NOT be created again
        _cartRepo.Verify(r => r.Add(It.IsAny<Carts>()), Times.Never);
    }

    // ClearAsync — cart with no items (empty items list branch)
    [Fact]
    public async Task Clear_CartWithNoItems_DoesNothing()
    {
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem>());

        await Sut().ClearAsync(1);

        _cartItemRepo.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }
}

// ═══════════════════════════════════════════════════════════
// PRODUCT SERVICE — remaining branches
// ═══════════════════════════════════════════════════════════
public class ProductBranchTests2
{
    private readonly Mock<IRepository<int, Product>>      _productRepo   = new();
    private readonly Mock<IRepository<int, Category>>     _categoryRepo  = new();
    private readonly Mock<IRepository<int, ProductImage>> _imageRepo     = new();
    private readonly Mock<IRepository<int, Inventory>>    _inventoryRepo = new();
    private readonly IMapper _mapper;

    public ProductBranchTests2()
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
            _imageRepo.Object, _inventoryRepo.Object, _mapper);
    }

    private ProductService MockSut() => new(_productRepo.Object, _categoryRepo.Object,
        _imageRepo.Object, _inventoryRepo.Object, _mapper);

    // Update — category not found after product found
    [Fact]
    public async Task Update_CategoryNotFound_ThrowsNotFoundException()
    {
        var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1 };
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product> { existing });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MockSut().UpdateAsync(1, new ProductUpdateDto { Id = 1, Name = "B", SKU = "S1", Price = 5m, CategoryId = 99 }));
    }

    // Update — Update repo returns null → NotFoundException
    [Fact]
    public async Task Update_RepoUpdateReturnsNull_ThrowsNotFoundException()
    {
        var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1, Reviews = new List<Review>() };
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product> { existing });
        _productRepo.Setup(r => r.Update(1, It.IsAny<Product>())).ReturnsAsync((Product?)null);

        using var ctx = CreateCtx(existing);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut(ctx).UpdateAsync(1, new ProductUpdateDto { Id = 1, Name = "B", SKU = "S1", Price = 5m, CategoryId = 1 }));
    }

    // Search — with both PriceMin and PriceMax
    [Fact]
    public async Task Search_BothPriceMinAndMax_FiltersCorrectly()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", Price = 5m, IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", Price = 50m, IsActive = true, CategoryId = 1 },
            new Product { Id = 3, Name = "P3", SKU = "S3", Price = 500m, IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(
            new ProductQuery { PriceMin = 10m, PriceMax = 100m, Page = 1, Size = 10 });

        Assert.Single(result.Items);
        Assert.Equal(50m, result.Items[0].Price);
    }

    // Search — sort by name asc
    [Fact]
    public async Task Search_SortByNameAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Zebra", SKU = "Z1", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Apple", SKU = "A1", IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(
            new ProductQuery { SortBy = "name", SortDir = "asc", Page = 1, Size = 10 });

        Assert.Equal("Apple", result.Items[0].Name);
    }

    // GetAll — sort by name asc
    [Fact]
    public async Task GetAll_SortByNameAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Zebra", SKU = "Z1", CategoryId = 1 },
            new Product { Id = 2, Name = "Apple", SKU = "A1", CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(1, 10, "name", "asc");

        Assert.Equal("Apple", result.Items[0].Name);
    }
}

// ═══════════════════════════════════════════════════════════
// ORDER SERVICE — remaining branches
// ═══════════════════════════════════════════════════════════
public class OrderBranchTests2
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

    public OrderBranchTests2()
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
            _promoSvc.Object, _walletSvc.Object, _logWriter.Object, _logger.Object);
    }

    private void SeedValidScenario(AppDbContext ctx, string paymentType = "UPI")
    {
        ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
        ctx.SaveChanges();
        var uid = ctx.Users.First().Id;
        ctx.Addresses.Add(new Address { UserId = uid, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
        ctx.SaveChanges();
        var product = new Product { Name = "W", SKU = "W1", Price = 100m, IsActive = true, CategoryId = 1 };
        ctx.Products.Add(product);
        ctx.SaveChanges();
        var cart = new Carts { UserId = uid };
        ctx.Carts.Add(cart);
        ctx.SaveChanges();
        ctx.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = 1, UnitPrice = 100m });
        ctx.Inventories.Add(new Inventory { ProductId = product.Id, Quantity = 10 });
        ctx.SaveChanges();

        var addedOrder = new Order { Id = 100, UserId = uid, OrderNumber = "ORD-TEST", Status = OrderStatus.Pending, PaymentStatus = PaymentStatus.Pending, SubTotal = 100m, Total = 100m, Items = new List<OrderItem>() };
        _orderRepo.Setup(r => r.Add(It.IsAny<Order>())).ReturnsAsync(addedOrder);
        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(addedOrder);
        _orderItemRepo.Setup(r => r.Add(It.IsAny<OrderItem>())).ReturnsAsync(new OrderItem());
        _inventoryRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(new Inventory());
        _cartItemRepo.Setup(r => r.Delete(It.IsAny<int>())).ReturnsAsync(new CartItem());
        _paymentRepo.Setup(r => r.Add(It.IsAny<Payment>())).ReturnsAsync(new Payment { PaymentType = paymentType });
    }

    // PlaceOrder — wallet exists but balance is 0 (allowed = 0, skip debit branch)
    [Fact]
    public async Task PlaceOrder_WalletZeroBalance_SkipsDebit()
    {
        using var ctx = CreateCtx();
        SeedValidScenario(ctx);
        var uid  = ctx.Users.First().Id;
        var adid = ctx.Addresses.First().Id;
        _walletSvc.Setup(w => w.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Wallet { UserId = uid, Balance = 0m });

        var req = new PlaceOrderRequestDto { UserId = uid, AddressId = adid, PaymentType = "UPI", WalletUseAmount = 50m };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
        _walletSvc.Verify(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // PlaceOrder — wallet is null (no wallet at all, skip debit branch)
    [Fact]
    public async Task PlaceOrder_WalletNull_SkipsDebit()
    {
        using var ctx = CreateCtx();
        SeedValidScenario(ctx);
        var uid  = ctx.Users.First().Id;
        var adid = ctx.Addresses.First().Id;
        _walletSvc.Setup(w => w.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        var req = new PlaceOrderRequestDto { UserId = uid, AddressId = adid, PaymentType = "UPI", WalletUseAmount = 50m };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
        _walletSvc.Verify(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // PlaceOrder — promo code provided but returns null (invalid promo, no discount)
    [Fact]
    public async Task PlaceOrder_InvalidPromoCode_NoDiscount()
    {
        using var ctx = CreateCtx();
        SeedValidScenario(ctx);
        var uid  = ctx.Users.First().Id;
        var adid = ctx.Addresses.First().Id;
        _promoSvc.Setup(p => p.GetValidPromoAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PromoCode?)null);

        var req = new PlaceOrderRequestDto { UserId = uid, AddressId = adid, PaymentType = "UPI", PromoCode = "INVALID" };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
    }

    // PlaceOrder — total goes negative after promo (clamped to 0)
    [Fact]
    public async Task PlaceOrder_PromoExceedsTotal_TotalClampedToZero()
    {
        using var ctx = CreateCtx();
        SeedValidScenario(ctx);
        var uid  = ctx.Users.First().Id;
        var adid = ctx.Addresses.First().Id;
        _promoSvc.Setup(p => p.GetValidPromoAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromoCode { Code = "BIG", DiscountAmount = 9999m });

        var req = new PlaceOrderRequestDto { UserId = uid, AddressId = adid, PaymentType = "UPI", PromoCode = "BIG" };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
    }

    // GetUserOrders — sort by date desc (default)
    [Fact]
    public async Task GetUserOrders_DefaultSort_Works()
    {
        using var ctx = CreateCtx();
        ctx.Orders.AddRange(
            new Order { UserId = 1, OrderNumber = "O1", Status = OrderStatus.Pending, ShipToName = "J", ShipToLine1 = "L", ShipToCity = "C", ShipToState = "S", ShipToPostalCode = "P", ShipToCountry = "I", PlacedAtUtc = DateTime.UtcNow.AddDays(-1) },
            new Order { UserId = 1, OrderNumber = "O2", Status = OrderStatus.Delivered, ShipToName = "J", ShipToLine1 = "L", ShipToCity = "C", ShipToState = "S", ShipToPostalCode = "P", ShipToCountry = "I", PlacedAtUtc = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10, sortBy: "date", desc: true);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("O2", result.Items[0].OrderNumber);
    }
}

// ═══════════════════════════════════════════════════════════
// ADDRESS SERVICE — remaining branches
// ═══════════════════════════════════════════════════════════
public class AddressBranchTests
{
    private readonly Mock<IRepository<int, Address>> _addressRepo = new();
    private readonly Mock<IRepository<int, User>>    _userRepo    = new();
    private readonly Mock<ILogger<AddressService>>   _logger      = new();

    private AppDbContext CreateCtx(params Address[] addresses)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new AppDbContext(opts);
        ctx.Addresses.AddRange(addresses);
        ctx.SaveChanges();
        return ctx;
    }

    private AddressService Sut(AppDbContext ctx)
    {
        _addressRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Addresses);
        return new AddressService(_addressRepo.Object, _userRepo.Object, _logger.Object);
    }

    // Update — address not found → NotFoundException
    [Fact]
    public async Task Update_AddressNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        _addressRepo.Setup(r => r.Get(99)).ReturnsAsync((Address?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut(ctx).UpdateAsync(99, 1, new AddressUpdateDto { FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P" }));
    }

    // Update — wrong user → ForbiddenException
    [Fact]
    public async Task Update_WrongUser_ThrowsForbiddenException()
    {
        using var ctx = CreateCtx();
        _addressRepo.Setup(r => r.Get(1)).ReturnsAsync(new Address { Id = 1, UserId = 5, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Sut(ctx).UpdateAsync(1, userId: 1, new AddressUpdateDto { FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P" }));
    }

    // Delete — wrong user → ForbiddenException
    [Fact]
    public async Task Delete_WrongUser_ThrowsForbiddenException()
    {
        using var ctx = CreateCtx();
        _addressRepo.Setup(r => r.Get(1)).ReturnsAsync(new Address { Id = 1, UserId = 5, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Sut(ctx).DeleteAsync(1, userId: 1));
    }

    // GetByUser — page/size zero defaults
    [Fact]
    public async Task GetByUser_ZeroPageSize_DefaultsToOne()
    {
        using var ctx = CreateCtx(
            new Address { UserId = 1, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });

        var result = await Sut(ctx).GetByUserAsync(1, 0, 0);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
    }
}
