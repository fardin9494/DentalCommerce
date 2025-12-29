using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class EfTransactionRunner : ITransactionRunner
{
    private readonly InventoryDbContext _db;

    public EfTransactionRunner(InventoryDbContext db) => _db = db;

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async token =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(token);
            try
            {
                await action(token);
                await tx.CommitAsync(token);
            }
            catch
            {
                try
                {
                    await tx.RollbackAsync(token);
                }
                catch
                {
                    // Swallow rollback failures to preserve original exception.
                }
                throw;
            }
        }, ct);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async token =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(token);
            try
            {
                var result = await action(token);
                await tx.CommitAsync(token);
                return result;
            }
            catch
            {
                try
                {
                    await tx.RollbackAsync(token);
                }
                catch
                {
                    // Swallow rollback failures to preserve original exception.
                }
                throw;
            }
        }, ct);
    }
}
