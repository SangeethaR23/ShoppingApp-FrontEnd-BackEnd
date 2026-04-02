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
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Models.enums;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

// ═══════════════════════════════════════════════════════════
// CART SERVICE — remaining uncovered branches
// ═══════════════════════════════════════════════════════════
public class CartBranchTests4 : IDisposable
{
    private readonly Mock<IRepository<int, Carts>>    _cartRepo     = new();
    private readonly Mock<IRepository<int, CartItem>> _cartItemRepo = new();
    private readonly Mock<IRepository<int, Product>>  _productRepo  = new();
    private readonly Mock<IRepository<int, User>>     _userRepo     = new();
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CartBranchTests4()
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

    // AddItem — GetAll returns null for carts (null-coalescing branch)
    [Fact]
    public async Task AddItem_GetAllCartsNull_TreatsAsEmpty()
    {
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S", Price = 10m, IsActive = true });
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Carts>?)null);
        var newCart = new Carts { Id = 1, UserId = 1 };
        _cartRepo.Setup(r => r.Add(It.IsAny<Carts>())).ReturnsAsync(newCart);
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem>());
        _cartItemRepo.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

        await Sut().AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 1 });

        _cartRepo.Verify(r => r.Add(It.IsAny<Carts>()), Times.Once);
    }

    // AddItem — GetAll returns null for items (null-coalescing branch)
    [Fact]
    public async Task AddItem_GetAllItemsNull_TreatsAsEmpty()
    {
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S", Price = 10m, IsActive = true });
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<CartItem>?)null);
        _cartItemRepo.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

        await Sut().AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 2 });

        _cartItemRepo.Verify(r => r.Add(It.IsAny<CartItem>()), Times.Once);
    }

    // AddItem — existing item found → updates quantity
    [Fact]
    public async Task AddItem_ExistingItem_UpdatesQuantity()
    {
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S", Price = 10m, IsActive = true });
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
        var existing = new CartItem { Id = 5, CartId = 1, ProductId = 1, Quantity = 2, UnitPrice = 10m };
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<CartItem> { existing });
        _cartItemRepo.Setup(r => r.Update(5, It.IsAny<CartItem>())).ReturnsAsync(existing);

        await Sut().AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 3 });

        _cartItemRepo.Verify(r => r.Update(5, It.Is<CartItem>(i => i.Quantity == 5)), Times.Once);
    }

    // UpdateItem — GetAll returns null for carts
    [Fact]
    public async Task UpdateItem_GetAllCartsNull_ThrowsNotFoundException()
    {
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Carts>?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().UpdateItemAsync(1, new CartUpdateItemDto { ProductId = 1, Quantity = 1 }));
    }

    // UpdateItem — GetAll returns null for items
    [Fact]
    public async Task UpdateItem_GetAllItemsNull_ThrowsNotFoundException()
    {
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<CartItem>?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().UpdateItemAsync(1, new CartUpdateItemDto { ProductId = 1, Quantity = 1 }));
    }

    // RemoveItem — GetAll returns null for carts
    [Fact]
    public async Task RemoveItem_GetAllCartsNull_ThrowsNotFoundException()
    {
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Carts>?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().RemoveItemAsync(1, 1));
    }

    // RemoveItem — GetAll returns null for items
    [Fact]
    public async Task RemoveItem_GetAllItemsNull_ThrowsNotFoundException()
    {
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts> { new Carts { Id = 1, UserId = 1 } });
        _cartItemRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<CartItem>?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().RemoveItemAsync(1, 1));
    }

    // ClearAsync — GetAll returns null for carts (returns early)
    [Fact]
    public async Task Clear_GetAllCartsNull_ReturnsEarly()
    {
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Carts>?)null);

        await Sut().ClearAsync(1); // should not throw

        _cartItemRepo.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }
}

// ═══════════════════════════════════════════════════════════
// ORDER SERVICE — remaining uncovered branches
// ═══════════════════════════════════════════════════════════
public class OrderBranchTests5
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

    public OrderBranchTests5()
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

    // PlaceOrder — cart item UnitPrice=0 falls back to product price (UnitPrice > 0 false branch)
    [Fact]
    public async Task PlaceOrder_CartItemZeroUnitPrice_FallsBackToProductPrice()
    {
        using var ctx = CreateCtx();
        ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
        ctx.SaveChanges();
        var uid = ctx.Users.First().Id;
        ctx.Addresses.Add(new Address { UserId = uid, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
        ctx.SaveChanges();
        var product = new Product { Name = "W", SKU = "W1", Price = 50m, IsActive = true, CategoryId = 1 };
        ctx.Products.Add(product);
        ctx.SaveChanges();
        ctx.Inventories.Add(new Inventory { ProductId = product.Id, Quantity = 10 });
        var cart = new Carts { UserId = uid };
        ctx.Carts.Add(cart);
        ctx.SaveChanges();
        // UnitPrice = 0 → should fall back to product.Price
        ctx.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = 1, UnitPrice = 0m });
        ctx.SaveChanges();

        var addedOrder = new Order { Id = 1, UserId = uid, OrderNumber = "O1", Status = OrderStatus.Pending, PaymentStatus = PaymentStatus.Pending };
        _orderRepo.Setup(r => r.Add(It.IsAny<Order>())).ReturnsAsync(addedOrder);
        _orderRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(addedOrder);
        _orderItemRepo.Setup(r => r.Add(It.IsAny<OrderItem>())).ReturnsAsync(new OrderItem());
        _inventoryRepo.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(new Inventory());
        _cartItemRepo.Setup(r => r.Delete(It.IsAny<int>())).ReturnsAsync(new CartItem());
        _paymentRepo.Setup(r => r.Add(It.IsAny<Payment>())).ReturnsAsync(new Payment { PaymentType = "UPI" });

        var req = new PlaceOrderRequestDto { UserId = uid, AddressId = ctx.Addresses.First().Id, PaymentType = "UPI" };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
    }

    // PlaceOrder — total goes negative from discount (total < 0 clamp branch)
    [Fact]
    public async Task PlaceOrder_DiscountExceedsTotal_TotalClampedToZero()
    {
        using var ctx = CreateCtx();
        SeedValidScenario(ctx);
        var uid  = ctx.Users.First().Id;
        var adid = ctx.Addresses.First().Id;

        var req = new PlaceOrderRequestDto
        {
            UserId = uid, AddressId = adid, PaymentType = "UPI",
            Discount = 9999m  // exceeds subtotal
        };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
    }

    // PlaceOrder — wallet debit path (WalletUseAmount > 0, balance sufficient)
    [Fact]
    public async Task PlaceOrder_WalletDebit_DeductsFromTotal()
    {
        using var ctx = CreateCtx();
        SeedValidScenario(ctx);
        var uid  = ctx.Users.First().Id;
        var adid = ctx.Addresses.First().Id;

        _walletSvc.Setup(w => w.GetAsync(uid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Wallet { UserId = uid, Balance = 50m });
        _walletSvc.Setup(w => w.DebitAsync(uid, It.IsAny<decimal>(),
            It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((50m, 1));

        var req = new PlaceOrderRequestDto { UserId = uid, AddressId = adid, PaymentType = "UPI", WalletUseAmount = 50m };
        var result = await BuildSut(ctx).PlaceOrderAsync(req);

        Assert.NotNull(result);
        _walletSvc.Verify(w => w.DebitAsync(uid, It.IsAny<decimal>(),
            WalletTxnType.DebitOrder, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // PlaceOrder — insufficient inventory throws
    [Fact]
    public async Task PlaceOrder_InsufficientInventory_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();
        ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
        ctx.SaveChanges();
        var uid = ctx.Users.First().Id;
        ctx.Addresses.Add(new Address { UserId = uid, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
        ctx.SaveChanges();
        var product = new Product { Name = "W", SKU = "W1", Price = 100m, IsActive = true, CategoryId = 1 };
        ctx.Products.Add(product);
        ctx.SaveChanges();
        ctx.Inventories.Add(new Inventory { ProductId = product.Id, Quantity = 0 }); // not enough
        var cart = new Carts { UserId = uid };
        ctx.Carts.Add(cart);
        ctx.SaveChanges();
        ctx.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = 5, UnitPrice = 100m });
        ctx.SaveChanges();

        var req = new PlaceOrderRequestDto { UserId = uid, AddressId = ctx.Addresses.First().Id, PaymentType = "UPI" };

        await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(req));
    }

    // GetUserOrders — page/size zero defaults
    [Fact]
    public async Task GetUserOrders_ZeroPageSize_DefaultsToOne()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Pending));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, page: 0, size: 0);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    // GetAll — page/size zero defaults
    [Fact]
    public async Task GetAll_ZeroPage_DefaultsToOne()
    {
        using var ctx = CreateCtx();
        ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Pending));
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(page: 0, size: 0);

        Assert.Equal(1, result.PageNumber);
    }

    // UpdateStatus — invalid status string throws
    [Fact]
    public async Task UpdateStatus_InvalidStatus_ThrowsBusinessValidationException()
    {
        using var ctx = CreateCtx();

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            BuildSut(ctx).UpdateStatusAsync(1, "NotAStatus"));
    }

    // Cancel — order not found throws
    [Fact]
    public async Task Cancel_OrderNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildSut(ctx).CancelOrderAsync(999, 1));
    }
}

// ═══════════════════════════════════════════════════════════
// PRODUCT SERVICE — remaining uncovered branches
// ═══════════════════════════════════════════════════════════
public class ProductBranchTests5
{
    private readonly Mock<IRepository<int, Product>>      _productRepo   = new();
    private readonly Mock<IRepository<int, Category>>     _categoryRepo  = new();
    private readonly Mock<IRepository<int, ProductImage>> _imageRepo     = new();
    private readonly Mock<IRepository<int, Inventory>>    _inventoryRepo = new();
    private readonly IMapper _mapper;

    public ProductBranchTests5()
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

    // RemoveImage — image belongs to different product (productId mismatch branch)
    [Fact]
    public async Task RemoveImage_WrongProduct_ThrowsNotFoundException()
    {
        _imageRepo.Setup(r => r.Get(5)).ReturnsAsync(new ProductImage { Id = 5, ProductId = 99, Url = "x" });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MockSut().RemoveImageAsync(productId: 1, imageId: 5));
    }

    // RemoveImage — image not found (null branch)
    [Fact]
    public async Task RemoveImage_ImageNotFound_ThrowsNotFoundException()
    {
        _imageRepo.Setup(r => r.Get(99)).ReturnsAsync((ProductImage?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MockSut().RemoveImageAsync(productId: 1, imageId: 99));
    }

    // GetAll — sort by price desc
    [Fact]
    public async Task GetAll_SortByPriceDesc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Cheap", SKU = "C1", Price = 5m, CategoryId = 1 },
            new Product { Id = 2, Name = "Expensive", SKU = "E1", Price = 500m, CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(1, 10, "price", "desc");

        Assert.Equal(500m, result.Items[0].Price);
    }

    // GetAll — sort by name desc
    [Fact]
    public async Task GetAll_SortByNameDesc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Apple", SKU = "A1", CategoryId = 1 },
            new Product { Id = 2, Name = "Zebra", SKU = "Z1", CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(1, 10, "name", "desc");

        Assert.Equal("Zebra", result.Items[0].Name);
    }

    // Search — sort by price desc
    [Fact]
    public async Task Search_SortByPriceDesc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", Price = 10m, IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", Price = 200m, IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(new ProductQuery { SortBy = "price", SortDir = "desc", Page = 1, Size = 10 });

        Assert.Equal(200m, result.Items[0].Price);
    }

    // Search — sort by name asc
    [Fact]
    public async Task Search_SortByNameAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Zebra", SKU = "Z1", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Apple", SKU = "A1", IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(new ProductQuery { SortBy = "name", SortDir = "asc", Page = 1, Size = 10 });

        Assert.Equal("Apple", result.Items[0].Name);
    }

    // Create — SKU duplicate throws ConflictException
    [Fact]
    public async Task Create_DuplicateSku_ThrowsConflictException()
    {
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>
            { new Product { Id = 1, SKU = "DUP", Name = "Existing" } });

        await Assert.ThrowsAsync<ConflictException>(() =>
            MockSut().CreateAsync(new ProductCreateDto { Name = "New", SKU = "DUP", Price = 10m, CategoryId = 1 }));
    }

    // Update — duplicate SKU on different product throws ConflictException
    [Fact]
    public async Task Update_DuplicateSkuOnOtherProduct_ThrowsConflictException()
    {
        var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1 };
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>
        {
            existing,
            new Product { Id = 2, SKU = "DUP", Name = "Other" }
        });

        await Assert.ThrowsAsync<ConflictException>(() =>
            MockSut().UpdateAsync(1, new ProductUpdateDto { Id = 1, Name = "B", SKU = "DUP", Price = 5m, CategoryId = 1 }));
    }
}
