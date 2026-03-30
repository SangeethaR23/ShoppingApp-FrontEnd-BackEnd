using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.enums;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class WalletServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly WalletService _sut;

        public WalletServiceTests()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _db = new AppDbContext(opts);
            _sut = new WalletService(_db);
        }

        public void Dispose() => _db.Dispose();

        private async Task<Wallet> SeedWallet(int userId, decimal balance)
        {
            var wallet = new Wallet { UserId = userId, Balance = balance };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();
            return wallet;
        }

        // ──────────────────────────────────────────────
        // GET
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Get_WalletExists_ReturnsWallet()
        {
            await SeedWallet(1, 500m);

            var result = await _sut.GetAsync(1);

            Assert.NotNull(result);
            Assert.Equal(500m, result!.Balance);
        }

        [Fact]
        public async Task Get_WalletNotExists_ReturnsNull()
        {
            var result = await _sut.GetAsync(99);
            Assert.Null(result);
        }

        // ──────────────────────────────────────────────
        // CREDIT
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Credit_ZeroAmount_ThrowsBusinessValidation()
        {
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.CreditAsync(1, 0m, WalletTxnType.CreditRefund));
        }

        [Fact]
        public async Task Credit_NegativeAmount_ThrowsBusinessValidation()
        {
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.CreditAsync(1, -50m, WalletTxnType.CreditRefund));
        }

        [Fact]
        public async Task Credit_WalletNotExists_AutoCreatesWalletAndCredits()
        {
            var (newBalance, txId) = await _sut.CreditAsync(99, 200m, WalletTxnType.AdminAdjust);

            Assert.Equal(200m, newBalance);
            Assert.True(txId > 0);
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == 99);
            Assert.NotNull(wallet);
            Assert.Equal(200m, wallet!.Balance);
        }

        [Fact]
        public async Task Credit_ExistingWallet_IncreasesBalance()
        {
            await SeedWallet(1, 100m);

            var (newBalance, _) = await _sut.CreditAsync(1, 50m, WalletTxnType.CreditRefund);

            Assert.Equal(150m, newBalance);
        }

        [Fact]
        public async Task Credit_CreatesTransactionRecord()
        {
            await SeedWallet(1, 100m);

            var (_, txId) = await _sut.CreditAsync(1, 75m, WalletTxnType.CreditRefund, "REF-001", "Test credit");

            var tx = await _db.WalletTransactions.FindAsync(txId);
            Assert.NotNull(tx);
            Assert.Equal(75m, tx!.Amount);
            Assert.Equal("REF-001", tx.Reference);
            Assert.Equal("Test credit", tx.Remarks);
        }

        // ──────────────────────────────────────────────
        // DEBIT
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Debit_ZeroAmount_ThrowsBusinessValidation()
        {
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.DebitAsync(1, 0m, WalletTxnType.DebitOrder));
        }

        [Fact]
        public async Task Debit_NegativeAmount_ThrowsBusinessValidation()
        {
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.DebitAsync(1, -10m, WalletTxnType.DebitOrder));
        }

        [Fact]
        public async Task Debit_WalletNotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.DebitAsync(99, 50m, WalletTxnType.DebitOrder));
        }

        [Fact]
        public async Task Debit_InsufficientBalance_ThrowsBusinessValidation()
        {
            await SeedWallet(1, 30m);

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _sut.DebitAsync(1, 100m, WalletTxnType.DebitOrder));
        }

        [Fact]
        public async Task Debit_SufficientBalance_ReducesBalance()
        {
            await SeedWallet(1, 500m);

            var (newBalance, _) = await _sut.DebitAsync(1, 200m, WalletTxnType.DebitOrder);

            Assert.Equal(300m, newBalance);
        }

        [Fact]
        public async Task Debit_ExactBalance_BringsToZero()
        {
            await SeedWallet(1, 100m);

            var (newBalance, _) = await _sut.DebitAsync(1, 100m, WalletTxnType.DebitOrder);

            Assert.Equal(0m, newBalance);
        }

        [Fact]
        public async Task Debit_CreatesNegativeTransactionRecord()
        {
            await SeedWallet(1, 200m);

            var (_, txId) = await _sut.DebitAsync(1, 75m, WalletTxnType.DebitOrder, "ORD-001");

            var tx = await _db.WalletTransactions.FindAsync(txId);
            Assert.NotNull(tx);
            Assert.Equal(-75m, tx!.Amount); // stored as negative
        }

        // ──────────────────────────────────────────────
        // CREDIT + DEBIT COMBO
        // ──────────────────────────────────────────────

        [Fact]
        public async Task CreditThenDebit_BalanceIsConsistent()
        {
            await SeedWallet(1, 0m);

            await _sut.CreditAsync(1, 1000m, WalletTxnType.AdminAdjust);
            await _sut.DebitAsync(1, 300m, WalletTxnType.DebitOrder);
            var (finalBalance, _) = await _sut.CreditAsync(1, 50m, WalletTxnType.CreditRefund);

            Assert.Equal(750m, finalBalance);
        }
    }
}
