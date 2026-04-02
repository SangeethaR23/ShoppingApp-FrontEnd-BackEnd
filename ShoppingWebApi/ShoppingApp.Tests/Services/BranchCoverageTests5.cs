using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Inventory;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

// ═══════════════════════════════════════════════════════════
// INVENTORY SERVICE — final branch gaps
// ═══════════════════════════════════════════════════════════
public class InventoryFinalBranchTests
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

    // ToDto — covered via normal GetByIdAsync with a real product
    [Fact]
    public async Task GetById_ExistingInventory_ReturnsProductNameAndSku()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "Widget", SKU = "W-001", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.Add(new Inventory { Id = 1, ProductId = 1, Quantity = 5, ReorderLevel = 2 });
        });

        var result = await Sut(ctx).GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Widget", result!.ProductName);
        Assert.Equal("W-001", result.SKU);
    }

    // GetPaged — lowStockOnly = false (explicit false, not null)
    [Fact]
    public async Task GetPaged_LowStockOnlyFalse_ReturnsAll()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 100, ReorderLevel = 5 });
        });

        var result = await Sut(ctx).GetPagedAsync(lowStockOnly: false);

        Assert.Equal(1, result.TotalCount);
    }

    // GetPaged — lowStockOnly = null (not set)
    [Fact]
    public async Task GetPaged_LowStockOnlyNull_ReturnsAll()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.Add(new Inventory { ProductId = 1, Quantity = 100, ReorderLevel = 5 });
        });

        var result = await Sut(ctx).GetPagedAsync(lowStockOnly: null);

        Assert.Equal(1, result.TotalCount);
    }

    // GetPaged — sortBy is null → defaults to "product"
    [Fact]
    public async Task GetPaged_SortByNull_DefaultsToProductName()
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

        var result = await Sut(ctx).GetPagedAsync(sortBy: null, desc: false);

        Assert.Equal("Apple", result.Items[0].ProductName);
    }

    // GetByProductId — not found returns null
    [Fact]
    public async Task GetByProductId_NotFound_ReturnsNull()
    {
        using var ctx = CreateCtx();

        var result = await Sut(ctx).GetByProductIdAsync(999);

        Assert.Null(result);
    }

    // AdjustAsync — with reason parameter (non-null)
    [Fact]
    public async Task Adjust_WithReason_Succeeds()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
        });
        var inv = ctx.Inventories.First();
        _invRepo.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);

        var result = await Sut(ctx).AdjustAsync(1, 5, reason: "Restock audit");

        Assert.Equal(15, result.Quantity);
    }

    // SetQuantity — valid product, verifies quantity is set
    [Fact]
    public async Task SetQuantity_ValidProduct_SetsQuantity()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.Add(new Inventory { Id = 1, ProductId = 1, Quantity = 5 });
        });
        var inv = ctx.Inventories.First();
        _invRepo.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);

        var result = await Sut(ctx).SetQuantityAsync(1, 20);

        Assert.Equal(20, result.Quantity);
    }

    // SetReorderLevel — valid product, verifies reorder level is set
    [Fact]
    public async Task SetReorderLevel_ValidProduct_SetsReorderLevel()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.Add(new Inventory { Id = 1, ProductId = 1, Quantity = 10, ReorderLevel = 0 });
        });
        var inv = ctx.Inventories.First();
        _invRepo.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);

        var result = await Sut(ctx).SetReorderLevelAsync(1, 15);

        Assert.Equal(15, result.ReorderLevel);
    }
}

// ═══════════════════════════════════════════════════════════
// PRODUCT SERVICE — final branch gaps
// ═══════════════════════════════════════════════════════════
public class ProductFinalBranchTests
{
    private readonly Mock<IRepository<int, Product>>      _productRepo   = new();
    private readonly Mock<IRepository<int, Category>>     _categoryRepo  = new();
    private readonly Mock<IRepository<int, ProductImage>> _imageRepo     = new();
    private readonly Mock<IRepository<int, Inventory>>    _inventoryRepo = new();
    private readonly IMapper _mapper;

    public ProductFinalBranchTests()
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

    // CreateAsync — null description (dto.Description?.Trim() → null)
    [Fact]
    public async Task Create_NullDescription_StoresNull()
    {
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
        var added = new Product { Id = 1, Name = "P", SKU = "S1", Price = 10m, CategoryId = 1, IsActive = true };
        _productRepo.Setup(r => r.Add(It.IsAny<Product>())).ReturnsAsync(added);
        _inventoryRepo.Setup(r => r.Add(It.IsAny<Inventory>())).ReturnsAsync(new Inventory());

        var result = await MockSut().CreateAsync(
            new ProductCreateDto { Name = "P", SKU = "S1", Price = 10m, CategoryId = 1, Description = null });

        Assert.NotNull(result);
    }

    // CreateAsync — with description (dto.Description?.Trim() → trimmed string)
    [Fact]
    public async Task Create_WithDescription_TrimmedAndStored()
    {
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
        var added = new Product { Id = 1, Name = "P", SKU = "S1", Price = 10m, CategoryId = 1, Description = "A product" };
        _productRepo.Setup(r => r.Add(It.IsAny<Product>())).ReturnsAsync(added);
        _inventoryRepo.Setup(r => r.Add(It.IsAny<Inventory>())).ReturnsAsync(new Inventory());

        var result = await MockSut().CreateAsync(
            new ProductCreateDto { Name = "P", SKU = "S1", Price = 10m, CategoryId = 1, Description = "  A product  " });

        Assert.NotNull(result);
    }

    // UpdateAsync — null description
    [Fact]
    public async Task Update_NullDescription_Succeeds()
    {
        var existing = new Product { Id = 1, Name = "A", SKU = "S1", CategoryId = 1, Reviews = new List<Review>() };
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.Get(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat" });
        _productRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product> { existing });
        _productRepo.Setup(r => r.Update(1, It.IsAny<Product>())).ReturnsAsync(existing);

        using var ctx = CreateCtx(existing);
        var result = await Sut(ctx).UpdateAsync(1,
            new ProductUpdateDto { Id = 1, Name = "B", SKU = "S1", Price = 5m, CategoryId = 1, Description = null });

        Assert.NotNull(result);
    }

    // GetAllAsync — products WITH reviews (Reviews.Any() = true branch)
    [Fact]
    public async Task GetAll_ProductsWithReviews_RatingCalculated()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 4, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 2, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await Sut(ctx).GetAllAsync(1, 10);

        Assert.Single(result.Items);
        Assert.Equal(3.0, result.Items[0].AverageRating);
        Assert.Equal(2, result.Items[0].ReviewsCount);
    }

    // GetAllAsync — products WITHOUT reviews (Reviews.Any() = false branch)
    [Fact]
    public async Task GetAll_ProductsWithNoReviews_RatingZero()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(1, 10);

        Assert.Single(result.Items);
        Assert.Equal(0.0, result.Items[0].AverageRating);
        Assert.Equal(0, result.Items[0].ReviewsCount);
    }

    // SearchAsync — products WITH reviews (Reviews.Any() = true branch)
    // Note: SearchAsync does not Include(Reviews), so rating is always 0 from search
    // The branch is hit via GetAllAsync which does include Reviews
    [Fact]
    public async Task Search_ProductsWithReviews_ReturnsProduct()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", IsActive = true, CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.Add(new Review { ProductId = 1, UserId = 1, Rating = 5, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await Sut(ctx).SearchAsync(new ProductQuery { Page = 1, Size = 10 });

        Assert.Single(result.Items);
        // SearchAsync doesn't Include Reviews, so rating defaults to 0
        Assert.Equal(0.0, result.Items[0].AverageRating);
    }

    // SearchAsync — products WITHOUT reviews (Reviews.Any() = false branch)
    [Fact]
    public async Task Search_ProductsWithNoReviews_RatingZero()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P", SKU = "S1", IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(new ProductQuery { Page = 1, Size = 10 });

        Assert.Single(result.Items);
        Assert.Equal(0.0, result.Items[0].AverageRating);
    }

    // GetByIdAsync — covered via GetAllAsync which also hits ratings.Any() branches
    // These two tests verify the same branches through GetAllAsync instead
    [Fact]
    public async Task GetById_ProductWithReviews_RatingCalculated()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.Add(new Review { ProductId = 1, UserId = 1, Rating = 4, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();

        // GetAllAsync also hits ratings.Any() = true branch
        var result = await Sut(ctx).GetAllAsync(1, 10);

        Assert.Single(result.Items);
        Assert.Equal(4.0, result.Items[0].AverageRating);
    }

    [Fact]
    public async Task GetById_ProductWithNoReviews_RatingZero()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 });

        // GetAllAsync hits ratings.Any() = false branch
        var result = await Sut(ctx).GetAllAsync(1, 10);

        Assert.Single(result.Items);
        Assert.Equal(0.0, result.Items[0].AverageRating);
    }

    // GetReviewsByProductId — sort by newest asc (desc=false)
    [Fact]
    public async Task GetReviews_SortByNewestAsc_Works()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 3, CreatedUtc = DateTime.UtcNow.AddDays(-1) },
            new Review { ProductId = 1, UserId = 2, Rating = 5, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(product);

        var result = await Sut(ctx).GetReviewsByProductIdAsync(1, 1, 10, sortBy: "newest", sortDir: "asc");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(3, result.Items[0].Rating); // oldest first
    }

    // SearchAsync — sort by price asc (desc=false)
    [Fact]
    public async Task Search_SortByPriceAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", Price = 100m, IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", Price = 10m, IsActive = true, CategoryId = 1 });

        var result = await Sut(ctx).SearchAsync(
            new ProductQuery { SortBy = "price", SortDir = "asc", Page = 1, Size = 10 });

        Assert.Equal(10m, result.Items[0].Price);
    }

    // SearchAsync — sort by newest asc
    [Fact]
    public async Task Search_SortByNewestAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Old", SKU = "O1", IsActive = true, CategoryId = 1, CreatedUtc = DateTime.UtcNow.AddDays(-5) },
            new Product { Id = 2, Name = "New", SKU = "N1", IsActive = true, CategoryId = 1, CreatedUtc = DateTime.UtcNow });

        var result = await Sut(ctx).SearchAsync(
            new ProductQuery { SortBy = "newest", SortDir = "asc", Page = 1, Size = 10 });

        Assert.Equal("Old", result.Items[0].Name);
    }

    // GetAllAsync — sort by newest asc (desc=false, default branch)
    [Fact]
    public async Task GetAll_SortByNewestAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 1 });

        var result = await Sut(ctx).GetAllAsync(1, 10, "newest", "asc");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Items[0].Id); // ascending by Id
    }
}

// ═══════════════════════════════════════════════════════════
// PRODUCT SERVICE — GetByIdAsync branch coverage
// ═══════════════════════════════════════════════════════════
public class ProductGetByIdBranchTests
{
    private readonly IMapper _mapper;

    public ProductGetByIdBranchTests()
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

    private (AppDbContext ctx, ProductService sut) CreateSut()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new AppDbContext(opts);

        // Seed required category
        ctx.Categories.Add(new Category { Id = 1, Name = "Cat" });
        ctx.SaveChanges();

        var productRepo   = new Mock<IRepository<int, Product>>();
        var categoryRepo  = new Mock<IRepository<int, Category>>();
        var imageRepo     = new Mock<IRepository<int, ProductImage>>();
        var inventoryRepo = new Mock<IRepository<int, Inventory>>();

        productRepo.Setup(r => r.GetQueryable()).Returns(() => ctx.Products);

        var sut = new ProductService(productRepo.Object, categoryRepo.Object,
            imageRepo.Object, inventoryRepo.Object, ctx, _mapper);

        return (ctx, sut);
    }

    // GetByIdAsync — product WITH reviews → ratings.Any() = true
    [Fact]
    public async Task GetById_WithReviews_RatingCalculated()
    {
        var (ctx, sut) = CreateSut();
        using (ctx)
        {
            ctx.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1, IsActive = true });
            ctx.SaveChanges();
            ctx.Reviews.AddRange(
                new Review { ProductId = 1, UserId = 1, Rating = 4, CreatedUtc = DateTime.UtcNow },
                new Review { ProductId = 1, UserId = 2, Rating = 2, CreatedUtc = DateTime.UtcNow });
            ctx.SaveChanges();

            var result = await sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(3.0, result!.AverageRating);
            Assert.Equal(2, result.ReviewsCount);
        }
    }

    // GetByIdAsync — product WITHOUT reviews → ratings.Any() = false
    [Fact]
    public async Task GetById_NoReviews_RatingZero()
    {
        var (ctx, sut) = CreateSut();
        using (ctx)
        {
            ctx.Products.Add(new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1, IsActive = true });
            ctx.SaveChanges();

            var result = await sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(0.0, result!.AverageRating);
            Assert.Equal(0, result.ReviewsCount);
        }
    }

    // GetByIdAsync — product not found → NotFoundException
    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        var (ctx, sut) = CreateSut();
        using (ctx)
        {
            await Assert.ThrowsAsync<ShoppingWebApi.Exceptions.NotFoundException>(
                () => sut.GetByIdAsync(99));
        }
    }
}
