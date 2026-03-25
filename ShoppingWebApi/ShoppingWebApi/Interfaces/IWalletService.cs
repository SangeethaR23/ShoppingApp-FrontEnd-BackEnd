using ShoppingWebApi.Models;
using ShoppingWebApi.Models.enums;

namespace ShoppingWebApi.Interfaces
{
    public interface IWalletService
    {

        Task<Wallet?> GetAsync(int userId, CancellationToken ct = default);

        Task<(decimal newBalance, int txId)> CreditAsync(
            int userId, decimal amount, WalletTxnType type,
            string? reference = null, string? remarks = null,
            CancellationToken ct = default);

        Task<(decimal newBalance, int txId)> DebitAsync(
            int userId, decimal amount, WalletTxnType type,
            string? reference = null, string? remarks = null,
            CancellationToken ct = default);

    }
}
