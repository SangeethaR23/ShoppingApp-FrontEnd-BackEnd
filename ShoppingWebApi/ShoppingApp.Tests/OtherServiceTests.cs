using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Inventory;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    // ══════════════════════════════════════════════
    // INVENTORY SERVICE TESTS
    // ══════════════════════════════════════════════
    public class InventoryServiceTests
    {
        private readonly Mock<IRepository<int, Inventory>> _inventoryRepoMock;
        private readonly Mock<IRepository<int, Product>> _productRepoMock;
        private readonly Mock<ILogger<InventoryService>> _loggerMock;

        public InventoryServiceTests()
        {
            _inventoryRepoMock = new Mock<IRepository<int, Inventory>>();
            _productRepoMock = new Mock<IRepository<int, Product>>();
            _loggerMock = new Mock<ILogger<InventoryService>>();
        }

        private AppDbContext CreateCtx(params (Product product, Inventory inventory)[] seeds)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new AppDbContext(opts);
            foreach (var (p, i) in seeds)
            {
                ctx.Products.Add(p);
                ctx.SaveChanges();
                i.ProductId = p.Id;
                ctx.Inventories.Add(i);
                ctx.SaveChanges();
            }
            return ctx;
        }

        private InventoryService BuildSut(AppDbContext ctx)
        {
            _inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Inventories);
            return new InventoryService(_inventoryRepoMock.Object, _productRepoMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetById_Found_ReturnsDto()
        {
            using var ctx = CreateCtx((new Product { Name = "P", SKU = "S1", CategoryId = 1 },
                                       new Inventory { Quantity = 50, ReorderLevel = 5 }));
            var inv = ctx.Inventories.First();
            var sut = BuildSut(ctx);

            var result = await sut.GetByIdAsync(inv.Id);

            Assert.NotNull(result);
            Assert.Equal(50, result!.Quantity);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNull()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            var result = await sut.GetByIdAsync(99);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByProductId_Found_ReturnsDto()
        {
            using var ctx = CreateCtx((new Product { Name = "P", SKU = "S1", CategoryId = 1 },
                                       new Inventory { Quantity = 20 }));
            var inv = ctx.Inventories.First();
            var sut = BuildSut(ctx);

            var result = await sut.GetByProductIdAsync(inv.ProductId);

            Assert.NotNull(result);
            Assert.Equal(20, result!.Quantity);
        }

        [Fact]
        public async Task GetPaged_LowStockFilter_OnlyReturnsLowStock()
        {
            using var ctx = CreateCtx(
                (new Product { Name = "P1", SKU = "S1", CategoryId = 1 }, new Inventory { Quantity = 2, ReorderLevel = 5 }),
                (new Product { Name = "P2", SKU = "S2", CategoryId = 1 }, new Inventory { Quantity = 100, ReorderLevel = 5 })
            );
            var sut = BuildSut(ctx);

            var result = await sut.GetPagedAsync(lowStockOnly: true);

            Assert.Single(result.Items);
            Assert.Equal(2, result.Items[0].Quantity);
        }

        [Fact]
        public async Task AdjustStock_NegativeDelta_ThrowsWhenInsufficient()
        {
            using var ctx = CreateCtx((new Product { Name = "P", SKU = "S1", CategoryId = 1 },
                                       new Inventory { Quantity = 5 }));
            var inv = ctx.Inventories.First();
            _inventoryRepoMock.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<BusinessValidationException>(() => sut.AdjustAsync(inv.ProductId, -10));
        }

        [Fact]
        public async Task AdjustStock_InventoryNotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<NotFoundException>(() => sut.AdjustAsync(99, 5));
        }

        [Fact]
        public async Task AdjustStock_Positive_Success()
        {
            using var ctx = CreateCtx((new Product { Name = "P", SKU = "S1", CategoryId = 1 },
                                       new Inventory { Quantity = 10 }));
            var inv = ctx.Inventories.First();
            _inventoryRepoMock.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);
            var sut = BuildSut(ctx);

            var result = await sut.AdjustAsync(inv.ProductId, 5);

            Assert.Equal(15, result.Quantity);
        }

        [Fact]
        public async Task AdjustStock_ExactNegative_BringsToZero()
        {
            using var ctx = CreateCtx((new Product { Name = "P", SKU = "S1", CategoryId = 1 },
                                       new Inventory { Quantity = 10 }));
            var inv = ctx.Inventories.First();
            _inventoryRepoMock.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);
            var sut = BuildSut(ctx);

            var result = await sut.AdjustAsync(inv.ProductId, -10);

            Assert.Equal(0, result.Quantity);
        }

        [Fact]
        public async Task SetQuantity_Negative_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<BusinessValidationException>(() => sut.SetQuantityAsync(1, -1));
        }

        [Fact]
        public async Task SetQuantity_NotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<NotFoundException>(() => sut.SetQuantityAsync(99, 10));
        }

        [Fact]
        public async Task SetQuantity_Success()
        {
            using var ctx = CreateCtx((new Product { Name = "P", SKU = "S1", CategoryId = 1 },
                                       new Inventory { Quantity = 5 }));
            var inv = ctx.Inventories.First();
            _inventoryRepoMock.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);
            var sut = BuildSut(ctx);

            var result = await sut.SetQuantityAsync(inv.ProductId, 100);

            Assert.Equal(100, result.Quantity);
        }

        [Fact]
        public async Task SetReorderLevel_Negative_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<BusinessValidationException>(() => sut.SetReorderLevelAsync(1, -5));
        }

        [Fact]
        public async Task SetReorderLevel_NotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            var sut = BuildSut(ctx);

            await Assert.ThrowsAsync<NotFoundException>(() => sut.SetReorderLevelAsync(99, 5));
        }

        [Fact]
        public async Task SetReorderLevel_Success()
        {
            using var ctx = CreateCtx((new Product { Name = "P", SKU = "S1", CategoryId = 1 },
                                       new Inventory { Quantity = 10, ReorderLevel = 0 }));
            var inv = ctx.Inventories.First();
            _inventoryRepoMock.Setup(r => r.Update(inv.Id, It.IsAny<Inventory>())).ReturnsAsync(inv);
            var sut = BuildSut(ctx);

            var result = await sut.SetReorderLevelAsync(inv.ProductId, 20);

            Assert.Equal(20, result.ReorderLevel);
        }
    }
}
// ══════════════════════════════════════════════
// PROMO SERVICE TESTS
// ══════════════════════════════════════════════
public class PromoServiceTests
{
    private readonly Mock<IRepository<int, PromoCode>> _promoRepoMock;

    public PromoServiceTests()
    {
        _promoRepoMock = new Mock<IRepository<int, PromoCode>>();
    }

    private AppDbContext CreateCtx(params PromoCode[] promos)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new AppDbContext(opts);
        ctx.PromoCodes.AddRange(promos);
        ctx.SaveChanges();
        return ctx;
    }

    private PromoService BuildSut(AppDbContext ctx)
    {
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.PromoCodes);
        return new PromoService(_promoRepoMock.Object);
    }

    [Fact]
    public async Task GetValidPromo_ValidCode_ReturnsPromo()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "SAVE100",
            DiscountAmount = 100m,
            IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-1),
            EndDateUtc = DateTime.UtcNow.AddDays(1),
            MinOrderAmount = 200m
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("SAVE100", 500m);

        Assert.NotNull(result);
        Assert.Equal(100m, result!.DiscountAmount);
    }

    [Fact]
    public async Task GetValidPromo_CodeNotFound_ReturnsNull()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("BADCODE", 500m);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidPromo_Inactive_ReturnsNull()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "OFF50",
            DiscountAmount = 50m,
            IsActive = false,
            StartDateUtc = DateTime.UtcNow.AddDays(-1),
            EndDateUtc = DateTime.UtcNow.AddDays(1)
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("OFF50", 500m);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidPromo_Expired_ReturnsNull()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "OLD50",
            DiscountAmount = 50m,
            IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-10),
            EndDateUtc = DateTime.UtcNow.AddDays(-1)
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("OLD50", 500m);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidPromo_NotStarted_ReturnsNull()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "FUTURE",
            DiscountAmount = 50m,
            IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(1),
            EndDateUtc = DateTime.UtcNow.AddDays(5)
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("FUTURE", 500m);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidPromo_CartBelowMinimum_ReturnsNull()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "MIN500",
            DiscountAmount = 100m,
            IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-1),
            EndDateUtc = DateTime.UtcNow.AddDays(1),
            MinOrderAmount = 500m
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("MIN500", 100m);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidPromo_NoMinOrderAmount_AnyCartTotal_Returns()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "FREE",
            DiscountAmount = 10m,
            IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-1),
            EndDateUtc = DateTime.UtcNow.AddDays(1),
            MinOrderAmount = null
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("FREE", 1m);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreatePromo_UppercasesCode()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        var promo = new PromoCode { Id = 1, Code = "SAVE10", DiscountAmount = 10m, IsActive = true };
        _promoRepoMock.Setup(r => r.Add(It.IsAny<PromoCode>())).ReturnsAsync(promo);

        await sut.CreateAsync(new ShoppingWebApi.Models.DTOs.Promo.PromoCreateDto
        {
            Code = "save10",
            DiscountAmount = 10m,
            StartDateUtc = DateTime.UtcNow,
            EndDateUtc = DateTime.UtcNow.AddDays(7)
        });

        _promoRepoMock.Verify(r => r.Add(It.Is<PromoCode>(p => p.Code == "SAVE10")), Times.Once);
    }

    [Fact]
    public async Task Activate_NotFound_ReturnsFalse()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _promoRepoMock.Setup(r => r.Get(99)).ReturnsAsync((PromoCode?)null);

        var result = await sut.ActivateAsync(99, false);
        Assert.False(result);
    }

    [Fact]
    public async Task Activate_Success_ReturnsTrue()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        var promo = new PromoCode { Id = 1, Code = "X", DiscountAmount = 10m, IsActive = true };
        _promoRepoMock.Setup(r => r.Get(1)).ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.Update(1, It.IsAny<PromoCode>())).ReturnsAsync(promo);

        var result = await sut.ActivateAsync(1, false);

        Assert.True(result);
        Assert.False(promo.IsActive);
    }
}

// ══════════════════════════════════════════════
// WISHLIST SERVICE TESTS
// ══════════════════════════════════════════════
public class WishlistServiceTests
{
    private readonly Mock<IRepository<int, WishlistItem>> _wishlistRepoMock;
    private readonly Mock<IRepository<int, Product>> _productRepoMock;
    private readonly Mock<IRepository<int, Carts>> _cartRepoMock;
    private readonly Mock<IRepository<int, CartItem>> _cartItemRepoMock;

    public WishlistServiceTests()
    {
        _wishlistRepoMock = new Mock<IRepository<int, WishlistItem>>();
        _productRepoMock = new Mock<IRepository<int, Product>>();
        _cartRepoMock = new Mock<IRepository<int, Carts>>();
        _cartItemRepoMock = new Mock<IRepository<int, CartItem>>();
    }

    private AppDbContext CreateCtx(Action<AppDbContext>? seed = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new AppDbContext(opts);
        seed?.Invoke(ctx);
        ctx.SaveChanges();
        return ctx;
    }

    private WishlistService BuildSut(AppDbContext ctx)
    {
        _wishlistRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.WishlistItems);
        _cartRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Carts);
        return new WishlistService(
            _wishlistRepoMock.Object, _productRepoMock.Object,
            _cartRepoMock.Object, _cartItemRepoMock.Object);
    }

    [Fact]
    public async Task Toggle_NotInWishlist_AddsItem_ReturnsTrue()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _wishlistRepoMock.Setup(r => r.Add(It.IsAny<WishlistItem>())).ReturnsAsync(new WishlistItem());

        var result = await sut.ToggleAsync(1, 1);

        Assert.True(result);
        _wishlistRepoMock.Verify(r => r.Add(It.IsAny<WishlistItem>()), Times.Once);
    }

    [Fact]
    public async Task Toggle_AlreadyInWishlist_RemovesItem_ReturnsFalse()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
            c.SaveChanges();
            c.WishlistItems.Add(new WishlistItem { Id = 1, UserId = 1, ProductId = 1 });
        });
        var existing = ctx.WishlistItems.First();
        _wishlistRepoMock.Setup(r => r.Delete(existing.Id)).ReturnsAsync(existing);
        var sut = BuildSut(ctx);

        var result = await sut.ToggleAsync(1, 1);

        Assert.False(result);
        _wishlistRepoMock.Verify(r => r.Delete(existing.Id), Times.Once);
    }

    [Fact]
    public async Task GetWishlist_ReturnsOnlyUserItems()
    {
        var product1 = new Product { Id = 1, Name = "P1", SKU = "S1", Price = 10m, IsActive = true, CategoryId = 1 };
        var product2 = new Product { Id = 2, Name = "P2", SKU = "S2", Price = 20m, IsActive = true, CategoryId = 1 };
        using var ctx = CreateCtx(c =>
        {
            c.Products.AddRange(product1, product2);
            c.SaveChanges();
            c.WishlistItems.AddRange(
                new WishlistItem { UserId = 1, ProductId = product1.Id },
                new WishlistItem { UserId = 1, ProductId = product2.Id },
                new WishlistItem { UserId = 2, ProductId = product1.Id }
            );
        });
        var sut = BuildSut(ctx);

        var result = (await sut.GetAsync(1)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task MoveToCart_NoCart_CreatesCartAndAddsItem()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        var newCart = new Carts { Id = 1, UserId = 1, Items = new List<CartItem>() };
        _cartRepoMock.Setup(r => r.Add(It.IsAny<Carts>())).ReturnsAsync(newCart);
        _productRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Product { Id = 1, Name = "P", SKU = "S", Price = 5m });
        _cartItemRepoMock.Setup(r => r.Add(It.IsAny<CartItem>())).ReturnsAsync(new CartItem());

        var result = await sut.MoveToCartAsync(1, 1);

        Assert.True(result);
        _cartRepoMock.Verify(r => r.Add(It.IsAny<Carts>()), Times.Once);
    }

    [Fact]
    public async Task MoveToCart_ExistingCartItem_IncreasesQuantity()
    {
        var cartItem = new CartItem { Id = 1, CartId = 1, ProductId = 1, Quantity = 1, UnitPrice = 5m };
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", PasswordHash = "x" });
            c.SaveChanges();
            c.Carts.Add(new Carts { Id = 1, UserId = 1 });
            c.SaveChanges();
            c.CartItems.Add(cartItem);
        });
        _cartItemRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<CartItem>())).ReturnsAsync(cartItem);
        var sut = BuildSut(ctx);

        var result = await sut.MoveToCartAsync(1, 1);

        Assert.True(result);
        Assert.Equal(2, cartItem.Quantity);
    }
}

// ══════════════════════════════════════════════
// USER SERVICE TESTS
// ══════════════════════════════════════════════
public class UserServiceTests
{
    private readonly Mock<IRepository<int, User>> _userRepoMock;
    private readonly Mock<IRepository<int, UserDetails>> _userDetailsRepoMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IRepository<int, User>>();
        _userDetailsRepoMock = new Mock<IRepository<int, UserDetails>>();
        _loggerMock = new Mock<ILogger<UserService>>();
    }

    private AppDbContext CreateCtx(params User[] users)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new AppDbContext(opts);
        ctx.Users.AddRange(users);
        ctx.SaveChanges();
        return ctx;
    }

    private UserService BuildSut(AppDbContext ctx)
    {
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Users);
        return new UserService(_userRepoMock.Object, _userDetailsRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_Found_ReturnsProfile()
    {
        using var ctx = CreateCtx(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" });
        var user = ctx.Users.First();
        var sut = BuildSut(ctx);

        var result = await sut.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("a@b.com", result!.Email);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);

        var result = await sut.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRole_EmptyRole_ThrowsBusinessValidation()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);

        await Assert.ThrowsAsync<BusinessValidationException>(() => sut.UpdateRoleAsync(1, ""));
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("SuperAdmin")]
    [InlineData("guest")]
    public async Task UpdateRole_InvalidRole_ThrowsBusinessValidation(string role)
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);

        await Assert.ThrowsAsync<BusinessValidationException>(() => sut.UpdateRoleAsync(1, role));
    }

    [Fact]
    public async Task UpdateRole_UserNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateRoleAsync(99, "Admin"));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    public async Task UpdateRole_ValidRole_ReturnsTrue(string role)
    {
        var user = new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" };
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Update(1, It.IsAny<User>())).ReturnsAsync(user);

        var result = await sut.UpdateRoleAsync(1, role);

        Assert.True(result);
        Assert.Equal(role, user.Role);
    }

    [Fact]
    public async Task ChangePassword_ShortNewPassword_ThrowsBusinessValidation()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        var dto = new ShoppingWebApi.Models.DTOs.Users.ChangePasswordRequestDto
        { UserId = 1, CurrentPassword = "old", NewPassword = "123" };

        await Assert.ThrowsAsync<BusinessValidationException>(() => sut.ChangePasswordAsync(dto));
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);
        var dto = new ShoppingWebApi.Models.DTOs.Users.ChangePasswordRequestDto
        { UserId = 99, CurrentPassword = "old123", NewPassword = "new123456" };

        await Assert.ThrowsAsync<NotFoundException>(() => sut.ChangePasswordAsync(dto));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ThrowsUnauthorized()
    {
        var hash = ShoppingWebApi.Services.Security.PasswordHasher.Hash("correct123");
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com", PasswordHash = hash });
        var dto = new ShoppingWebApi.Models.DTOs.Users.ChangePasswordRequestDto
        { UserId = 1, CurrentPassword = "wrongpass", NewPassword = "newpassword123" };

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => sut.ChangePasswordAsync(dto));
    }

    [Fact]
    public async Task ChangePassword_Success_ReturnsTrue()
    {
        var hash = ShoppingWebApi.Services.Security.PasswordHasher.Hash("oldpass123");
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = hash };
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Update(1, It.IsAny<User>())).ReturnsAsync(user);
        var dto = new ShoppingWebApi.Models.DTOs.Users.ChangePasswordRequestDto
        { UserId = 1, CurrentPassword = "oldpass123", NewPassword = "newpass456" };

        var result = await sut.ChangePasswordAsync(dto);
        Assert.True(result);
    }

    [Fact]
    public async Task GetPagedUsers_EmailFilter_Works()
    {
        using var ctx = CreateCtx(
            new User { Email = "alice@test.com", Role = "User", PasswordHash = "x" },
            new User { Email = "bob@test.com", Role = "Admin", PasswordHash = "x" }
        );
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync("alice", null, null, "date", true, 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("alice@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetPagedUsers_RoleFilter_Works()
    {
        using var ctx = CreateCtx(
            new User { Email = "alice@test.com", Role = "User", PasswordHash = "x" },
            new User { Email = "bob@test.com", Role = "Admin", PasswordHash = "x" }
        );
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, "Admin", null, "date", true, 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("bob@test.com", result.Items[0].Email);
    }
}

