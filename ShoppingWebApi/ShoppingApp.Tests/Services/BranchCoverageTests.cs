using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Cart;
using ShoppingWebApi.Models.DTOs.Categories;
using ShoppingWebApi.Models.DTOs.Inventory;
using ShoppingWebApi.Services;
using ShoppingWebApi.Services.Security;

namespace ShoppingApp.Tests.Services;

// ═══════════════════════════════════════════════════════════
// CATEGORY SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class CategoryBranchTests : IDisposable
{
    private readonly Mock<IRepository<int, Category>> _catRepo = new();
    private readonly Mock<IRepository<int, Product>>  _prodRepo = new();
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CategoryBranchTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new AppDbContext(opts);
        _mapper = new MapperConfiguration(c => c.CreateMap<Category, CategoryReadDto>()).CreateMapper();
    }

    public void Dispose() => _db.Dispose();

    private CategoryService Sut() => new(_catRepo.Object, _prodRepo.Object, _db, _mapper);

    // GetAllAsync — sort by createdutc asc/desc
    [Fact]
    public async Task GetAll_SortByCreatedUtcAsc_Works()
    {
        _db.Categories.AddRange(
            new Category { Name = "B", CreatedUtc = DateTime.UtcNow.AddDays(-1) },
            new Category { Name = "A", CreatedUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await Sut().GetAllAsync(1, 10, "createdutc", "asc");
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAll_SortByCreatedUtcDesc_Works()
    {
        _db.Categories.Add(new Category { Name = "X" });
        await _db.SaveChangesAsync();

        var result = await Sut().GetAllAsync(1, 10, "createdutc", "desc");
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAll_SortByUpdatedUtcAsc_Works()
    {
        _db.Categories.Add(new Category { Name = "Y", UpdatedUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await Sut().GetAllAsync(1, 10, "updatedutc", "asc");
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAll_SortByUpdatedUtcDesc_Works()
    {
        _db.Categories.Add(new Category { Name = "Z" });
        await _db.SaveChangesAsync();

        var result = await Sut().GetAllAsync(1, 10, "updatedutc", "desc");
        Assert.Equal(1, result.TotalCount);
    }

    // Update — no parent (null ParentCategoryId branch skipped)
    [Fact]
    public async Task Update_NoParent_Success()
    {
        var cat = new Category { Id = 1, Name = "Old" };
        _catRepo.Setup(r => r.Get(1)).ReturnsAsync(cat);
        _catRepo.Setup(r => r.Update(1, It.IsAny<Category>()))
            .ReturnsAsync(new Category { Id = 1, Name = "New" });

        var result = await Sut().UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "New" });
        Assert.Equal("New", result!.Name);
    }

    // Update — Update returns null → NotFoundException
    [Fact]
    public async Task Update_UpdateReturnsNull_ThrowsNotFoundException()
    {
        var cat = new Category { Id = 1, Name = "Cat" };
        _catRepo.Setup(r => r.Get(1)).ReturnsAsync(cat);
        _catRepo.Setup(r => r.Update(1, It.IsAny<Category>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "New" }));
    }

    // Update — deep cycle (ancestor chain walk)
    [Fact]
    public async Task Update_DeepCycle_ThrowsBusinessValidation()
    {
        // cat1 -> cat2 -> cat3 -> cat1 would be cycle
        var cat1 = new Category { Id = 1, Name = "C1" };
        var cat2 = new Category { Id = 2, Name = "C2", ParentCategoryId = 3 };
        var cat3 = new Category { Id = 3, Name = "C3", ParentCategoryId = 1 };

        _catRepo.Setup(r => r.Get(1)).ReturnsAsync(cat1);
        _catRepo.Setup(r => r.Get(2)).ReturnsAsync(cat2);
        _catRepo.Setup(r => r.Get(3)).ReturnsAsync(cat3);

        // Try to set cat1's parent to cat2 — walk: cat2.parent=3, cat3.parent=1 → cycle!
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            Sut().UpdateAsync(1, new CategoryUpdateDto { Id = 1, Name = "C1", ParentCategoryId = 2 }));
    }

    // Delete — Delete repo returns null → NotFoundException
    [Fact]
    public async Task Delete_DeleteReturnsNull_ThrowsNotFoundException()
    {
        var cat = new Category { Id = 1, Name = "Cat" };
        _catRepo.Setup(r => r.Get(1)).ReturnsAsync(cat);
        _catRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Category>());
        _prodRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Product>());
        _catRepo.Setup(r => r.Delete(1)).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().DeleteAsync(1));
    }
}

// ═══════════════════════════════════════════════════════════
// CART SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class CartBranchTests : IDisposable
{
    private readonly Mock<IRepository<int, Carts>>    _cartRepo     = new();
    private readonly Mock<IRepository<int, CartItem>> _cartItemRepo = new();
    private readonly Mock<IRepository<int, Product>>  _productRepo  = new();
    private readonly Mock<IRepository<int, User>>     _userRepo     = new();
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CartBranchTests()
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

    // AddItem — cartRepo.Add returns null → BusinessValidationException
    [Fact]
    public async Task AddItem_CartAddReturnsNull_ThrowsBusinessValidation()
    {
        _userRepo.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
        _productRepo.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S", Price = 5m, IsActive = true });
        _cartRepo.Setup(r => r.GetAll()).ReturnsAsync(new List<Carts>());
        _cartRepo.Setup(r => r.Add(It.IsAny<Carts>())).ReturnsAsync((Carts?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            Sut().AddItemAsync(1, new CartAddItemDto { ProductId = 1, Quantity = 1 }));
    }

    // GetByUserId — cart with items that have reviews (rating lookup branch)
    [Fact]
    public async Task GetByUserId_WithReviews_PopulatesRatings()
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S", Price = 10m, CategoryId = 1 };
        var cart    = new Carts { Id = 1, UserId = 2 };
        var item    = new CartItem { Id = 1, CartId = 1, ProductId = 1, Quantity = 1, UnitPrice = 10m, Product = product };
        cart.Items.Add(item);

        _db.Products.Add(product);
        _db.Carts.Add(cart);
        _db.CartItems.Add(item);
        _db.Reviews.Add(new Review { ProductId = 1, UserId = 1, Rating = 5, CreatedUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await Sut().GetByUserIdAsync(2);

        Assert.Single(result.Items);
        Assert.Equal(5.0, result.Items[0].AverageRating);
        Assert.Equal(1, result.Items[0].ReviewsCount);
    }

    // GetByUserId — cart with items but NO reviews (rating lookup returns nothing)
    [Fact]
    public async Task GetByUserId_WithItemsNoReviews_RatingIsZero()
    {
        var product = new Product { Id = 2, Name = "P2", SKU = "S2", Price = 20m, CategoryId = 1 };
        var cart    = new Carts { Id = 2, UserId = 3 };
        var item    = new CartItem { Id = 2, CartId = 2, ProductId = 2, Quantity = 1, UnitPrice = 20m, Product = product };
        cart.Items.Add(item);

        _db.Products.Add(product);
        _db.Carts.Add(cart);
        _db.CartItems.Add(item);
        await _db.SaveChangesAsync();

        var result = await Sut().GetByUserIdAsync(3);

        Assert.Single(result.Items);
        Assert.Equal(0.0, result.Items[0].AverageRating);
    }
}

// ═══════════════════════════════════════════════════════════
// INVENTORY SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class InventoryBranchTests
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

    // GetPaged — sort by quantity asc/desc
    [Fact]
    public async Task GetPaged_SortByQuantityAsc_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 50 },
                new Inventory { ProductId = 2, Quantity = 5 });
        });

        var result = await Sut(ctx).GetPagedAsync(sortBy: "quantity", desc: false);
        Assert.Equal(5, result.Items[0].Quantity);
    }

    [Fact]
    public async Task GetPaged_SortByQuantityDesc_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 5 },
                new Inventory { ProductId = 2, Quantity = 50 });
        });

        var result = await Sut(ctx).GetPagedAsync(sortBy: "quantity", desc: true);
        Assert.Equal(50, result.Items[0].Quantity);
    }

    // GetPaged — sort by reorderlevel
    [Fact]
    public async Task GetPaged_SortByReorderLevelAsc_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 10, ReorderLevel = 20 },
                new Inventory { ProductId = 2, Quantity = 10, ReorderLevel = 5 });
        });

        var result = await Sut(ctx).GetPagedAsync(sortBy: "reorderlevel", desc: false);
        Assert.Equal(5, result.Items[0].ReorderLevel);
    }

    [Fact]
    public async Task GetPaged_SortByReorderLevelDesc_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 10, ReorderLevel = 5 },
                new Inventory { ProductId = 2, Quantity = 10, ReorderLevel = 20 });
        });

        var result = await Sut(ctx).GetPagedAsync(sortBy: "reorderlevel", desc: true);
        Assert.Equal(20, result.Items[0].ReorderLevel);
    }

    // GetPaged — categoryId filter
    [Fact]
    public async Task GetPaged_FilterByCategoryId_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 2 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 10 },
                new Inventory { ProductId = 2, Quantity = 20 });
        });

        var result = await Sut(ctx).GetPagedAsync(categoryId: 1);
        Assert.Single(result.Items);
    }

    // GetPaged — sku filter
    [Fact]
    public async Task GetPaged_FilterBySku_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "P1", SKU = "WIDGET-001", CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "GADGET-002", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 10 },
                new Inventory { ProductId = 2, Quantity = 20 });
        });

        var result = await Sut(ctx).GetPagedAsync(sku: "WIDGET");
        Assert.Single(result.Items);
    }

    // GetPaged — productId filter
    [Fact]
    public async Task GetPaged_FilterByProductId_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(
                new Product { Id = 1, Name = "P1", SKU = "S1", CategoryId = 1 },
                new Product { Id = 2, Name = "P2", SKU = "S2", CategoryId = 1 });
            c.SaveChanges();
            c.Inventories.AddRange(
                new Inventory { ProductId = 1, Quantity = 10 },
                new Inventory { ProductId = 2, Quantity = 20 });
        });

        var result = await Sut(ctx).GetPagedAsync(productId: 1);
        Assert.Single(result.Items);
    }
}

// ═══════════════════════════════════════════════════════════
// PASSWORD HASHER — branch coverage
// ═══════════════════════════════════════════════════════════
public class PasswordHasherBranchTests
{
    [Fact]
    public void Verify_WrongVersionPrefix_ReturnsFalse()
    {
        // "v2.salt.hash" — wrong version
        Assert.False(PasswordHasher.Verify("pass", "v2.abc.def"));
    }

    [Fact]
    public void Verify_OnlyTwoParts_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("pass", "v1.onlytwoparts"));
    }

    [Fact]
    public void Verify_NullHash_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("pass", null!));
    }

    [Fact]
    public void Verify_WhitespaceHash_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("pass", "   "));
    }

    [Fact]
    public void Hash_ThenVerify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("MyPassword123");
        Assert.True(PasswordHasher.Verify("MyPassword123", hash));
    }

    [Fact]
    public void Hash_ThenVerify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("Correct");
        Assert.False(PasswordHasher.Verify("Wrong", hash));
    }
}

// ═══════════════════════════════════════════════════════════
// REVIEW SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class ReviewBranchTests
{
    private readonly Mock<IRepository<int, Review>>  _reviewRepo  = new();
    private readonly Mock<IRepository<int, Product>> _productRepo = new();
    private readonly Mock<IRepository<int, User>>    _userRepo    = new();
    private readonly Mock<ILogger<ReviewService>>    _logger      = new();
    private readonly IMapper _mapper;

    public ReviewBranchTests()
    {
        _mapper = new MapperConfiguration(c =>
            c.CreateMap<Review, ShoppingWebApi.Models.DTOs.Reviews.ReviewReadDto>()
             .ForMember(d => d.UserName, o => o.Ignore()))
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

    // Update — comment is null (optional branch)
    [Fact]
    public async Task Update_NullComment_Success()
    {
        var review = new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 3 };
        using var ctx = CreateCtx(review);
        _reviewRepo.Setup(r => r.Update(1, It.IsAny<Review>())).ReturnsAsync(review);

        var result = await Sut(ctx).UpdateAsync(1, 1,
            new ShoppingWebApi.Models.DTOs.Reviews.ReviewUpdateDto { Rating = 4, Comment = null });

        Assert.True(result);
    }

    // Update — comment provided
    [Fact]
    public async Task Update_WithComment_UpdatesComment()
    {
        var review = new Review { Id = 1, ProductId = 1, UserId = 1, Rating = 3 };
        using var ctx = CreateCtx(review);
        _reviewRepo.Setup(r => r.Update(1, It.IsAny<Review>())).ReturnsAsync(review);

        await Sut(ctx).UpdateAsync(1, 1,
            new ShoppingWebApi.Models.DTOs.Reviews.ReviewUpdateDto { Rating = 5, Comment = "Updated comment" });

        Assert.Equal("Updated comment", review.Comment);
    }

    // GetByProduct — size < 1 defaults to 1
    [Fact]
    public async Task GetByProduct_SizeZero_DefaultsToOne()
    {
        using var ctx = CreateCtx(
            new Review { ProductId = 1, UserId = 1, Rating = 4, CreatedUtc = DateTime.UtcNow });

        var result = await Sut(ctx).GetByProductAsync(1, 1, 0);

        Assert.Equal(1, result.PageSize);
    }
}

// ═══════════════════════════════════════════════════════════
// WISHLIST SERVICE — branch coverage
// ═══════════════════════════════════════════════════════════
public class WishlistBranchTests
{
    private readonly Mock<IRepository<int, WishlistItem>> _wishlistRepo  = new();
    private readonly Mock<IRepository<int, Product>>      _productRepo   = new();
    private readonly Mock<IRepository<int, Carts>>        _cartRepo      = new();
    private readonly Mock<IRepository<int, CartItem>>     _cartItemRepo  = new();

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

    // MoveToCart — product not found (null product branch)
    [Fact]
    public async Task MoveToCart_ProductNotFound_StillReturnsTrue()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
            c.SaveChanges();
            c.Carts.Add(new Carts { Id = 1, UserId = 1 });
        });

        _productRepo.Setup(r => r.Get(99)).ReturnsAsync((Product?)null);
        _cartItemRepo.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

        // When product is null, UnitPrice defaults to 0 — still completes
        var result = await Sut(ctx).MoveToCartAsync(1, 99);
        Assert.True(result);
    }

    // MoveToCart — existing cart, new item (no existing cart item)
    [Fact]
    public async Task MoveToCart_ExistingCart_NewItem_AddsItem()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
            c.SaveChanges();
            c.Carts.Add(new Carts { Id = 1, UserId = 1 });
        });

        _productRepo.Setup(r => r.Get(5)).ReturnsAsync(new Product { Id = 5, Name = "P", SKU = "S", Price = 10m });
        _cartItemRepo.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

        var result = await Sut(ctx).MoveToCartAsync(1, 5);

        Assert.True(result);
        _cartItemRepo.Verify(r => r.Add(It.IsAny<CartItem>()), Times.Once);
    }
}
