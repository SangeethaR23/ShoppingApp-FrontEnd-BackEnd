using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingApp.Tests.Helpers;
using ShoppingWebApi.Controllers;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.enums;

namespace ShoppingApp.Tests.Controllers;

public class WalletControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IWalletService> _svcMock = new();
    private readonly WalletController _sut;

    public WalletControllerTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new WalletController(_svcMock.Object, _db);
        ControllerTestHelper.SetUser(_sut, userId: 1);
    }

    public void Dispose() => _db.Dispose();

    // ── GetMyWallet ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyWallet_WalletExists_Returns200WithBalance()
    {
        _db.Wallets.Add(new Wallet { Id = 1, UserId = 1, Balance = 500m });
        await _db.SaveChangesAsync();
        _svcMock.Setup(s => s.GetAsync(1, default)).ReturnsAsync(new Wallet { Id = 1, UserId = 1, Balance = 500m });

        var result = await _sut.GetMyWallet(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetMyWallet_NoWallet_Returns200WithZeroBalance()
    {
        _svcMock.Setup(s => s.GetAsync(1, default)).ReturnsAsync((Wallet?)null);

        var result = await _sut.GetMyWallet(default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
        dynamic val = result.Value!;
        Assert.Equal(0m, (decimal)val.GetType().GetProperty("balance")!.GetValue(val)!);
    }

    [Fact]
    public async Task GetMyWallet_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.GetMyWallet(default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── GetTransactions ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_NoWallet_Returns200WithEmptyItems()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        // No wallet seeded for userId=1

        var result = await _sut.GetTransactions(1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_WithWalletAndTxns_Returns200WithItems()
    {
        ControllerTestHelper.SetUser(_sut, 2);
        var wallet = new Wallet { Id = 10, UserId = 2, Balance = 200m };
        _db.Wallets.Add(wallet);
        _db.WalletTransactions.Add(new WalletTransaction
        {
            Id = 1, WalletId = 10, UserId = 2, Amount = 100m,
            Type = WalletTxnType.AdminAdjust, CreatedUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetTransactions(1, 10, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.GetTransactions(1, 10, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetTransactions_Pagination_Returns200()
    {
        ControllerTestHelper.SetUser(_sut, 3);
        var wallet = new Wallet { Id = 20, UserId = 3, Balance = 100m };
        _db.Wallets.Add(wallet);
        for (int i = 1; i <= 5; i++)
        {
            _db.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = 20, UserId = 3, Amount = 10m * i,
                Type = WalletTxnType.AdminAdjust, CreatedUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        var result = await _sut.GetTransactions(page: 1, size: 2, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    // ── CreditWallet ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditWallet_ValidAmount_Returns200WithNewBalance()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.CreditAsync(1, 100m, WalletTxnType.AdminAdjust, null, "User top-up", default))
            .ReturnsAsync((600m, 1));

        var result = await _sut.CreditWallet(new WalletCreditRequest { Amount = 100m }, default) as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task CreditWallet_ZeroAmount_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);

        var result = await _sut.CreditWallet(new WalletCreditRequest { Amount = 0m }, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task CreditWallet_NegativeAmount_Returns400()
    {
        ControllerTestHelper.SetUser(_sut, 1);

        var result = await _sut.CreditWallet(new WalletCreditRequest { Amount = -50m }, default) as BadRequestObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task CreditWallet_NoUserClaim_Returns401()
    {
        ControllerTestHelper.SetAnonymousUser(_sut);

        var result = await _sut.CreditWallet(new WalletCreditRequest { Amount = 100m }, default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreditWallet_CallsServiceWithCorrectArgs()
    {
        ControllerTestHelper.SetUser(_sut, 1);
        _svcMock.Setup(s => s.CreditAsync(1, 250m, WalletTxnType.AdminAdjust, null, "User top-up", default))
            .ReturnsAsync((750m, 2));

        await _sut.CreditWallet(new WalletCreditRequest { Amount = 250m }, default);

        _svcMock.Verify(s => s.CreditAsync(1, 250m, WalletTxnType.AdminAdjust, null, "User top-up", default), Times.Once);
    }
}
