using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Promo;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

public class PromoServiceAdditionalTests
{
    private readonly Mock<IRepository<int, PromoCode>> _promoRepoMock = new();

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

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllPromos()
    {
        using var ctx = CreateCtx(
            new PromoCode { Code = "A", DiscountAmount = 10m, IsActive = true, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(1) },
            new PromoCode { Code = "B", DiscountAmount = 20m, IsActive = false, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(1) });
        var sut = BuildSut(ctx);

        var result = (await sut.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmpty()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);

        var result = await sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_OrderedByCreatedUtcDesc()
    {
        using var ctx = CreateCtx(
            new PromoCode { Code = "OLD", DiscountAmount = 5m, IsActive = true, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(1), CreatedUtc = DateTime.UtcNow.AddDays(-5) },
            new PromoCode { Code = "NEW", DiscountAmount = 5m, IsActive = true, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(1), CreatedUtc = DateTime.UtcNow });
        var sut = BuildSut(ctx);

        var result = (await sut.GetAllAsync()).ToList();

        Assert.Equal("NEW", result[0].Code);
    }

    // ── CreateAsync — edge cases ──────────────────────────────────────────────

    [Fact]
    public async Task Create_SetsIsActiveTrue()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        var created = new PromoCode { Id = 1, Code = "TEST", DiscountAmount = 10m, IsActive = true };
        _promoRepoMock.Setup(r => r.Add(It.IsAny<PromoCode>())).ReturnsAsync(created);

        var result = await sut.CreateAsync(new PromoCreateDto
        {
            Code = "test", DiscountAmount = 10m,
            StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(7)
        });

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task Create_WithMinOrderAmount_Persists()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);
        PromoCode? captured = null;
        _promoRepoMock.Setup(r => r.Add(It.IsAny<PromoCode>()))
            .Callback<PromoCode>(p => captured = p)
            .ReturnsAsync(new PromoCode { Id = 1, Code = "MIN", DiscountAmount = 50m, IsActive = true });

        await sut.CreateAsync(new PromoCreateDto
        {
            Code = "MIN", DiscountAmount = 50m, MinOrderAmount = 500m,
            StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(7)
        });

        Assert.Equal(500m, captured!.MinOrderAmount);
    }

    // ── GetValidPromoAsync — boundary cases ───────────────────────────────────

    [Fact]
    public async Task GetValidPromo_ExactMinOrderAmount_ReturnsPromo()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "EXACT", DiscountAmount = 10m, IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-1), EndDateUtc = DateTime.UtcNow.AddDays(1),
            MinOrderAmount = 100m
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("EXACT", 100m);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetValidPromo_JustBelowMinOrderAmount_ReturnsNull()
    {
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "BELOW", DiscountAmount = 10m, IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-1), EndDateUtc = DateTime.UtcNow.AddDays(1),
            MinOrderAmount = 100m
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("BELOW", 99.99m);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidPromo_ExactEndDate_ReturnsPromo()
    {
        // EndDateUtc is today — should still be valid
        using var ctx = CreateCtx(new PromoCode
        {
            Code = "TODAY", DiscountAmount = 10m, IsActive = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-1),
            EndDateUtc = DateTime.UtcNow.AddMinutes(1) // just barely valid
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetValidPromoAsync("TODAY", 50m);

        Assert.NotNull(result);
    }

    // ── ActivateAsync — toggle ────────────────────────────────────────────────

    [Fact]
    public async Task Activate_TogglesIsActive()
    {
        using var ctx = CreateCtx();
        var promo = new PromoCode { Id = 1, Code = "X", DiscountAmount = 5m, IsActive = true };
        _promoRepoMock.Setup(r => r.Get(1)).ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.Update(1, It.IsAny<PromoCode>())).ReturnsAsync(promo);
        var sut = BuildSut(ctx);

        await sut.ActivateAsync(1, false);

        Assert.False(promo.IsActive);
    }

    [Fact]
    public async Task Activate_SetsUpdatedUtc()
    {
        using var ctx = CreateCtx();
        var promo = new PromoCode { Id = 1, Code = "X", DiscountAmount = 5m, IsActive = false };
        _promoRepoMock.Setup(r => r.Get(1)).ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.Update(1, It.IsAny<PromoCode>())).ReturnsAsync(promo);
        var sut = BuildSut(ctx);

        await sut.ActivateAsync(1, true);

        Assert.NotNull(promo.UpdatedUtc);
    }
}
