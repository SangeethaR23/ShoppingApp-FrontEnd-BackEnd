using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Common;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.enums;

namespace ShoppingWebApi.Services
{
    public class WalletService : IWalletService
    {
       
            private readonly AppDbContext _db;

            public WalletService(AppDbContext db)
            {
                _db = db;
            }

            public async Task<Wallet?> GetAsync(int userId, CancellationToken ct = default)
            {
                return await _db.Wallets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.UserId == userId, ct);
            }

            public async Task<(decimal newBalance, int txId)> CreditAsync(
                int userId, decimal amount, WalletTxnType type,
                string? reference = null, string? remarks = null,
                CancellationToken ct = default)
            {
                if (amount <= 0)
                    throw new BusinessValidationException("Credit amount must be positive.");

                // ? AUTO-CREATE WALLET IF NOT EXISTS
                var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
                if (wallet == null)
                {
                    wallet = new Wallet
                    {
                        UserId = userId,
                        Balance = 0m
                    };
                    _db.Wallets.Add(wallet);
                    await _db.SaveChangesAsync(ct);
                }

                using var tx = await _db.Database.BeginTransactionSafeAsync(ct);

                wallet.Balance += amount;
                wallet.UpdatedUtc = DateTime.UtcNow;
                _db.Wallets.Update(wallet);

                var entry = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    UserId = userId,
                    Amount = amount,
                    Type = type,
                    Reference = reference,
                    Remarks = remarks
                };

                _db.WalletTransactions.Add(entry);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return (wallet.Balance, entry.Id);
            }

            public async Task<(decimal newBalance, int txId)> DebitAsync(
                int userId, decimal amount, WalletTxnType type,
                string? reference = null, string? remarks = null,
                CancellationToken ct = default)
            {
                if (amount <= 0)
                    throw new BusinessValidationException("Debit amount must be positive.");

                // ? Wallet must exist for debit
                var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
                if (wallet == null)
                    throw new NotFoundException("Wallet not found for this user.");

                if (wallet.Balance < amount)
                    throw new BusinessValidationException("Insufficient wallet balance.");

                using var tx = await _db.Database.BeginTransactionSafeAsync(ct);

                wallet.Balance -= amount;
                wallet.UpdatedUtc = DateTime.UtcNow;
                _db.Wallets.Update(wallet);

                var entry = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    UserId = userId,
                    Amount = -amount,
                    Type = type,
                    Reference = reference,
                    Remarks = remarks
                };

                _db.WalletTransactions.Add(entry);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return (wallet.Balance, entry.Id);
            }
        
    }
}
