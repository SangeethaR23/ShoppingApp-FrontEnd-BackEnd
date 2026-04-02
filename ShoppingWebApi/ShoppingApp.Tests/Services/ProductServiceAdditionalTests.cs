using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Products;
using ShoppingWebApi.Models.DTOs.Reviews;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

public class ProductServiceAdditionalTests
{
    private readonly Mock<IRepository<int, Product>>      _productRepoMock   = new();
    private readonly Mock<IRepository<int, Category>>     _categoryRepoMock  = new();
    private readonly Mock<IRepository<int, ProductImage>> _imageRepoMock     = new();
    private readonly Mock<IRepository<int, Inventory>>    _inventoryRepoMock = new();
    private readonly IMapper _mapper;

    public ProductServiceAdditionalTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ProductImage, ProductImageReadDto>();
            cfg.CreateMap<Product, ProductReadDto>()
               .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images))
               .ForMember(d => d.AverageRating, opt => opt.Ignore())
               .ForMember(d => d.ReviewsCount, opt => opt.Ignore());
            cfg.CreateMap<Review, ReviewReadDto>()
               .ForMember(d => d.UserName, opt => opt.Ignore());
        });
        _mapper = config.CreateMapper();
    }

    private AppDbContext CreateCtx(params Product[] products)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new AppDbContext(opts);
        ctx.Products.AddRange(products);
        ctx.SaveChanges();
        return ctx;
    }

    private ProductService BuildSut(AppDbContext ctx)
    {
        _productRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Products);
        return new ProductService(_productRepoMock.Object, _categoryRepoMock.Object,
            _imageRepoMock.Object, _inventoryRepoMock.Object, ctx, _mapper);
    }

    // ── GetAllAsync — sort variants ───────────────────────────────────────────

    [Fact]
    public async Task GetAll_SortByNameAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Zebra", SKU = "Z1", CategoryId = 1 },
            new Product { Id = 2, Name = "Apple", SKU = "A1", CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.GetAllAsync(1, 10, "name", "asc");

        Assert.Equal("Apple", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAll_SortByNameDesc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Apple", SKU = "A1", CategoryId = 1 },
            new Product { Id = 2, Name = "Zebra", SKU = "Z1", CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.GetAllAsync(1, 10, "name", "desc");

        Assert.Equal("Zebra", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAll_SortByPriceDesc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Cheap", SKU = "C1", Price = 10m, CategoryId = 1 },
            new Product { Id = 2, Name = "Expensive", SKU = "E1", Price = 999m, CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.GetAllAsync(1, 10, "price", "desc");

        Assert.Equal(999m, result.Items[0].Price);
    }

    [Fact]
    public async Task GetAll_DefaultSort_ByIdDesc()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.GetAllAsync(1, 10);

        Assert.Equal(2, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAll_PageSizeZero_DefaultsToOne()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.GetAllAsync(0, 0);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
    }

    // ── SearchAsync — category + sort variants ────────────────────────────────

    [Fact]
    public async Task Search_ByCategory_FiltersCorrectly()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", IsActive = true, CategoryId = 2 });
        var sut = BuildSut(ctx);

        var result = await sut.SearchAsync(new ProductQuery { CategoryId = 1, Page = 1, Size = 10 });

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].CategoryId);
    }

    [Fact]
    public async Task Search_SortByPriceAsc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "P1", SKU = "S1", Price = 500m, IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "P2", SKU = "S2", Price = 10m, IsActive = true, CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.SearchAsync(new ProductQuery { SortBy = "price", SortDir = "asc", Page = 1, Size = 10 });

        Assert.Equal(10m, result.Items[0].Price);
    }

    [Fact]
    public async Task Search_SortByNameDesc_Works()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Alpha", SKU = "A1", IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Zeta", SKU = "Z1", IsActive = true, CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.SearchAsync(new ProductQuery { SortBy = "name", SortDir = "desc", Page = 1, Size = 10 });

        Assert.Equal("Zeta", result.Items[0].Name);
    }

    [Fact]
    public async Task Search_SortByNewest_DefaultsToCreatedUtcDesc()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Old", SKU = "O1", IsActive = true, CategoryId = 1, CreatedUtc = DateTime.UtcNow.AddDays(-5) },
            new Product { Id = 2, Name = "New", SKU = "N1", IsActive = true, CategoryId = 1, CreatedUtc = DateTime.UtcNow });
        var sut = BuildSut(ctx);

        var result = await sut.SearchAsync(new ProductQuery { SortBy = "newest", SortDir = "desc", Page = 1, Size = 10 });

        Assert.Equal("New", result.Items[0].Name);
    }

    [Fact]
    public async Task Search_PriceMinOnly_FiltersCorrectly()
    {
        using var ctx = CreateCtx(
            new Product { Id = 1, Name = "Cheap", SKU = "C1", Price = 5m, IsActive = true, CategoryId = 1 },
            new Product { Id = 2, Name = "Mid", SKU = "M1", Price = 50m, IsActive = true, CategoryId = 1 },
            new Product { Id = 3, Name = "Expensive", SKU = "E1", Price = 500m, IsActive = true, CategoryId = 1 });
        var sut = BuildSut(ctx);

        var result = await sut.SearchAsync(new ProductQuery { PriceMin = 40m, Page = 1, Size = 10 });

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Search_Pagination_Works()
    {
        using var ctx = CreateCtx(
            Enumerable.Range(1, 6).Select(i =>
                new Product { Id = i, Name = $"P{i}", SKU = $"S{i}", IsActive = true, CategoryId = 1 }).ToArray());
        var sut = BuildSut(ctx);

        var result = await sut.SearchAsync(new ProductQuery { Page = 2, Size = 2 });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(6, result.TotalCount);
    }

    // ── GetReviewsByProductId ─────────────────────────────────────────────────

    [Fact]
    public async Task GetReviews_ProductNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _productRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetReviewsByProductIdAsync(99, 1, 10));
    }

    [Fact]
    public async Task GetReviews_WithMinRating_FiltersCorrectly()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 2, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 5, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(product);
        var sut = BuildSut(ctx);

        var result = await sut.GetReviewsByProductIdAsync(1, 1, 10, minRating: 4);

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Rating);
    }

    [Fact]
    public async Task GetReviews_SortByRatingAsc_Works()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S1", CategoryId = 1 };
        using var ctx = CreateCtx(product);
        ctx.Reviews.AddRange(
            new Review { ProductId = 1, UserId = 1, Rating = 5, CreatedUtc = DateTime.UtcNow },
            new Review { ProductId = 1, UserId = 2, Rating = 1, CreatedUtc = DateTime.UtcNow });
        ctx.SaveChanges();
        _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(product);
        var sut = BuildSut(ctx);

        var result = await sut.GetReviewsByProductIdAsync(1, 1, 10, sortBy: "rating", sortDir: "asc");

        Assert.Equal(1, result.Items[0].Rating);
    }
}
