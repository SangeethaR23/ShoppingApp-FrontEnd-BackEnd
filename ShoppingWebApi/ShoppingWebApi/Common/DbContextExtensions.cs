using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ShoppingWebApi.Common
{
    public static class DbContextExtensions
    {
        public static async Task<IDbContextTransaction> BeginTransactionSafeAsync(
            this DatabaseFacade database, CancellationToken ct = default)
        {
            if (database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
                return new NoOpTransaction();

            return await database.BeginTransactionAsync(ct);
        }
    }

    internal sealed class NoOpTransaction : IDbContextTransaction
    {
        public Guid TransactionId => Guid.Empty;
        public void Commit() { }
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
