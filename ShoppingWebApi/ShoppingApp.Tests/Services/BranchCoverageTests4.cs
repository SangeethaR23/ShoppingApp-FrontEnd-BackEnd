using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Categories;
using ShoppingWebApi.Models.DTOs.Inventory;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Models.enums;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

// ═══════════════════════════════════════════════════════════
// PRODUCT SERVICE — remaining branch gaps
// ═══════════════════════════════════════════════════════════
public class ProductBranchTests3
{
    private readonly Mock<IRepository<int, Product>>      _productRepo   = new();
    private readonly Mock<IRepository<int, Category>>     _categoryRepo  = new();
    private readonly Mock<IRepository<int, ProductImage>> _imageRepo     = new();
    private readonly Mock<IRepository<int, Inventory>>    _inventoryRepo = new();
    private readonly IMapper _mapper;

    public ProductBranchTests3()
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

    // UpdateAsync — product has no reviews → ratings is null → ratings?.Avg ?? 0
    [Fact]
    public async Task Update_ProductWithNoReviews_RatingDefaultsToZero()
    {
        var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1, Reviews = new List<Review>() };
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product> { existing });
        _productRepo.Setup(r => r.Update(1, It.IsAny<Product>())).ReturnsAsync(existing);

        using var ctx = CreateCtx(existing);
        var result = await Sut(ctx).UpdateAsync(1,
            new ProductUpdateDto { Id = 1, Name = "B", SKU = "S1", Price = 5m, CategoryId = 1 });

        Assert.NotNull(result);
        Assert.Equal(0, result!.AverageRating);
        Assert.Equal(0, result.ReviewsCount);
    }

    // UpdateAsync — product has reviews → ratings populated
    [Fact]
    public async Task Update_ProductWithReviews_RatingCalculated()
    {
        var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1, Reviews = new List<Review>() };
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product> { existing });
        _productRepo.Setup(r => r.Update(1, It.IsAny<Product>())).ReturnsAsync(existing);

        using var ctx = CreateCtx(existing);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 4, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 2, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await Sut(ctx).UpdateAsync(1,
            new ProductUpdateDto { Id = 1, Name = "B", SKU = "S1", Price = 5m, CategoryId = 1 });

        Assert.NotNull(result);
        Assert.Equal(3.0, result!.AverageRating);
        Assert.Equal(2, result.ReviewsCount);
    }

    // SearchAsync — NameContains filter
    [Fact]
    public async Task Search_NameContains_FiltersCorrectly()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "iPhone 15", SKU = "IP15", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Samsung S24", SKU = "SS24", IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(
            new ProductQuery { NameContains = "iPhone", Page = 1, Size = 10 });

        Assert.Single(result.Items);
        Assert.Contains("iPhone", result.Items[0].Name);
    }

    // SearchAsync — NameContains with whitespace trimmed
    [Fact]
    public async Task Search_NameContainsWithSpaces_TrimmedAndFilters()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Laptop Pro", SKU = "LP1", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Desktop", SKU = "DT1", IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(
            new ProductQuery { NameContains = "  Laptop  ", Page = 1, Size = 10 });

        Assert.Single(result.Items);
    }

    // GetReviewsByProductId — no minRating filter (HasValue = false branch)
    [Fact]
    public async Task GetReviews_NoMinRating_ReturnsAll()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 1, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 5, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);

        var result = await Sut(ctx).GetReviewsByProductIdAsync(1, 1, 10, minRating: null);

        Assert.Equal(2, result.TotalCount);
    }

    // GetReviewsByProductId — sort by rating desc
    [Fact]
    public async Task GetReviews_SortByRatingDesc_Works()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 1, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 5, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);

        var result = await Sut(ctx).GetReviewsByProductIdAsync(1, 1, 10, sortBy: "rating", sortDir: "desc");

        Assert.Equal(5, result.Items[0].Rating);
    }

    // GetReviewsByProductId — sort by rating asc
    [Fact]
    public async Task GetReviews_SortByRatingAsc_Works()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 5, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 1, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);

        var result = await Sut(ctx).GetReviewsByProductIdAsync(1, 1, 10, sortBy: "rating", sortDir: "asc");

        Assert.Equal(1, result.Items[0].Rating);
    }

    // Create — GetAll returns null (null-coalescing branch)
    [Fact]
    public async Task Create_GetAllReturnsNull_TreatsAsEmpty()
    {
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Product>?)null);
        var added = new Product { Id = 10, Name = "W", SKU = "W1", Price = 10m, CategoryId = 1, IsActive = true };
        _productRepo.Setup(r => r.Add(It.IsAny<Product>())).ReturnsAsync(added);
        _inventoryRepo.Setup(r => r.Add(It.IsAny<Inventory>())).ReturnsAsync(new Inventory());

        var result = await MockSut().CreateAsync(
            new ProductCreateDto { Name = "W", SKU = "W1", Price = 10m, CategoryId = 1 });

        Assert.Equal("W", result.Name);
    }
}

// ═══════════════════════════════════════════════════════════
// INVENTORY SERVICE — remaining branch gaps
// ═══════════════════════════════════════════════════════════
public class InventoryBranchTests2
{
    private readonly Mock<IRepository<int, Inventory>> _invRepo     = new();
    private readonly Mock<IRepository<int, Product>>   _productRepo = new();
    private readonly Mock<ILogger<InventoryService>>   _logger      = new();

    private AppDbContext CreateCtx(Action<AppDbContext>? seed = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new AppDbContext(opts);
        seed?.Invoke(ctx);
        ctx.SaveChanges();
        return ctx;
    }

    private InventoryService Sut(AppDbContext ctx)
    {
        _invRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Inventories);
        return new InventoryService(_invRepo.Object, _productRepo.Object, _logger.Object);
    }

    // GetPaged — sort by product name desc
    [Fact]
    public async Task GetPaged_SortByProductDesc_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "Apple", SKU = "A1", CategoryId = 1 },
                new Product { Id = 2, Name = "Zebra", SKU = "Z1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 10 },
                new Inventory { ProductId = 2, Quantity = 20 });
        });

        var result = await Sut(ctx).GetPagedAsync(sortBy: "product", desc: true);

        Assert.Equal("Zebra", result.Items[0].ProductName);
    }

    // GetPaged — sort by product name asc (default)
    [Fact]
    public async Task GetPaged_SortByProductAsc_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "Zebra", SKU = "Z1", CategoryId = 1 },
                new Product { Id = 2, Name = "Apple", SKU = "A1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 10 },
                new Inventory { ProductId = 2, Quantity = 20 });
        });

        var result = await Sut(ctx).GetPagedAsync(sortBy: "product", desc: false);

        Assert.Equal("Apple", result.Items[0].ProductName);
    }

    // GetPaged — page/size zero defaults to 1
    [Fact]
    public async Task GetPaged_ZeroPageSize_DefaultsToOne()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.Add(new Inventory { ProductId = 1, Quantity = 5 });
        });

        var result = await Sut(ctx).GetPagedAsync(page: 0, size: 0);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
    }

    // AdjustAsync — delta = 0 (no change, still valid)
    [Fact]
    public async Task Adjust_ZeroDelta_QuantityUnchanged()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
        });
        var inv = ctx.Inventories.First();
        _invRepo.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);

        var result = await Sut(ctx).AdjustAsync(1, 0);

        Assert.Equal(10, result.Quantity);
    }
}

// ═══════════════════════════════════════════════════════════
// CATEGORY SERVICE — remaining branch gaps
// ═══════════════════════════════════════════════════════════
public class CategoryBranchTests2 : IDisposable
{
    private readonly Mock<IRepository<int, Category>> _catRepo  = new();
    private readonly Mock<IRepository<int, Product>>  _prodRepo = new();
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CategoryBranchTests2()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new AppDbContext(opts);
        _mapper = new MapperConfiguration(c => c.CreateMap<Category, CategoryReadDto>()).CreateMapper();
    }

    public void Dispose() => _db.Dispose();

    private CategoryService Sut() => new(_catRepo.Object, _prodRepo.Object, _db, _mapper);

    // Delete — GetAll returns null for categories (null-coalescing branch)
    [Fact]
    public async Task Delete_GetAllCategoriesNull_TreatsAsEmpty()
    {
        var cat = new Category { Id = 1, Name = "Cat" };
        _catRepo.Setup(r => r.Get(1)).ReturnsAsync(cat);
        _catRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Category>?)null);
        _prodRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
        _catRepo.Setup(r => r.Delete(1)).ReturnsAsync(cat);

        var result = await Sut().DeleteAsync(1);

        Assert.True(result);
    }

    // Delete — GetAll returns null for products (null-coalescing branch)
    [Fact]
    public async Task Delete_GetAllProductsNull_TreatsAsEmpty()
    {
        var cat = new Category { Id = 1, Name = "Cat" };
        _catRepo.Setup(r => r.Get(1)).ReturnsAsync(cat);
        _catRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Category>());
        _prodRepo.Setup(r => r.GetAll()).ReturnsAsync((IEnumerable<Product>?)null);
        _catRepo.Setup(r => r.Delete(1)).ReturnsAsync(cat);

        var result = await Sut().DeleteAsync(1);

        Assert.True(result);
    }

    // Update — parent has no further ancestors (single-level, no cycle walk needed)
    [Fact]
    public async Task Update_ParentWithNoAncestors_Success()
    {
        var cat    = new Category { Id = 1, Name = "Child" };
        var parent = new Category { Id = 2, Name = "Parent", ParentCategoryId = null };

        _catRepo.Setup(r => r.Get(1)).ReturnsAsync(cat);
        _catRepo.Setup(r => r.Get(2)).ReturnsAsync(parent);
        _catRepo.Setup(r => r.Update(1, It.IsAny<Category>()))
            .ReturnsAsync(new Category { Id = 1, Name = "Child", ParentCategoryId = 2 });

        var result = await Sut().UpdateAsync(1,
            new CategoryUpdateDto { Id = 1, Name = "Child", ParentCategoryId = 2 });

        Assert.NotNull(result);
    }

    // GetAll — sort by name desc
    [Fact]
    public async Task GetAll_SortByNameDesc_Works()
    {
        _db.Categories.AddRange(
            new Category { Name = "Apple" },
            new Category { Name = "Zebra" });
        await _db.SaveChangesAsync();

        var result = await Sut().GetAllAsync(1, 10, "name", "desc");

        Assert.Equal("Zebra", result.Items[0].Name);
    }
}

// ═══════════════════════════════════════════════════════════
// ORDER SERVICE — remaining branch gaps
// ═══════════════════════════════════════════════════════════
public class OrderBranchTests3
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

    public OrderBranchTests3()
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
        decimal total = 100m, List<OrderItem>? items = null) => new Order
        {
            UserId = userId, OrderNumber = num, Status = status, Total = total,
            ShipToName = "J", ShipToLine1 = "L", ShipToCity = "C",
            ShipToState = "S", ShipToPostalCode = "P", ShipToCountry = "I",
            Items = items ?? new List<OrderItem>()
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
            _promoSvc.Object, _walletSvc.Object, _logWriter.Object, _logger.Object);
    }

    // GetAll — sort by date asc
    [Fact]
    public async Task GetAll_SortByDateAsc_Works()
    {
        using var ctx = CreateCtx();
        var o1 = MakeOrder(1, "O1", OrderStatus.Pending);
        o1.PlacedAtUtc = DateTime.UtcNow.AddDays(-5);
        var o2 = MakeOrder(1, "O2", OrderStatus.Pending);
        o2.PlacedAtUtc = DateTime.UtcNow;
        ctx.Orders.AddRange(o1, o2);
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(sortBy: "date", desc: false, page: 1, size: 10);

        Assert.Equal("O1", result.Items[0].OrderNumber);
    }

    // GetAll — filter by To date
    [Fact]
    public async Task GetAll_FilterByToDate_Works()
    {
        using var ctx = CreateCtx();
        var old = MakeOrder(1, "O1", OrderStatus.Pending);
        old.PlacedAtUtc = DateTime.UtcNow.AddDays(-10);
        var recent = MakeOrder(1, "O2", OrderStatus.Pending);
        recent.PlacedAtUtc = DateTime.UtcNow;
        ctx.Orders.AddRange(old, recent);
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetAllAsync(
            to: DateTime.UtcNow.AddDays(-5), page: 1, size: 10);

        Assert.Single(result.Items);
        Assert.Equal("O1", result.Items[0].OrderNumber);
    }

    // GetUserOrders — sort by date asc
    [Fact]
    public async Task GetUserOrders_SortByDateAsc_Works()
    {
        using var ctx = CreateCtx();
        var o1 = MakeOrder(1, "O1", OrderStatus.Pending);
        o1.PlacedAtUtc = DateTime.UtcNow.AddDays(-3);
        var o2 = MakeOrder(1, "O2", OrderStatus.Delivered);
        o2.PlacedAtUtc = DateTime.UtcNow;
        ctx.Orders.AddRange(o1, o2);
        ctx.SaveChanges();

        var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10, sortBy: "date", desc: false);

        Assert.Equal("O1", result.Items[0].OrderNumber);
    }

    // UpdateStatus — Delivered status (valid transition)
    [Fact]
    public async Task UpdateStatus_ToDelivered_ReturnsTrue()
    {
        using var ctx = CreateCtx();
        var order = new Order { Id = 1, Status = OrderStatus.Shipped, UserId = 1, OrderNumber = "O1" };
        _orderRepo.Setup(r => r.Get(1)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.Update(1, It.IsAny<Order>())).ReturnsAsync(order);

        var result = await BuildSut(ctx).UpdateStatusAsync(1, "Delivered");

        Assert.True(result);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }
}
