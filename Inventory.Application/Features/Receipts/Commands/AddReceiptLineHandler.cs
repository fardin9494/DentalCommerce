using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class AddReceiptLineHandler : IRequestHandler<AddReceiptLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public AddReceiptLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(AddReceiptLineCommand req, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        const int maxAttempts = 3;

        return await strategy.ExecuteAsync(async () =>
        {
            DbUpdateConcurrencyException? lastEx = null;
            List<string> lastEntities = new();
            List<string> lastStates = new();
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var rec = await _db.Receipts
                        .Include(r => r.Lines)
                        .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                        ?? throw new InvalidOperationException("رسید پیدا نشد.");

                    var line = rec.AddLine(req.ProductId, req.VariantId, req.Qty, req.LotNumber, req.ExpiryDateUtc, req.UnitCost);
                    _db.Entry(line).State = EntityState.Added;

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return line.Id;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    lastEx = ex;
                    await tx.RollbackAsync(ct);

                    // تلاش مجدد پس از بارگذاری مجدد موجودیت‌هایی که در خطای همزمانی دخیل بوده‌اند
                    lastEntities = ex.Entries
                        .Select(e =>
                        {
                            var pkValues = e.Properties
                                .Where(p => p.Metadata.IsPrimaryKey())
                                .Select(p => $"{p.Metadata.Name}={p.CurrentValue}");
                            var pkText = string.Join(",", pkValues);
                            return $"{e.Metadata.Name}[{pkText}]";
                        })
                        .Distinct()
                        .ToList();
                    lastStates = ex.Entries
                        .Select(e => $"{e.Metadata.Name}:{e.State}")
                        .Distinct()
                        .ToList();

                    Console.Error.WriteLine($"[Concurrency] attempt {attempt} for ReceiptId={req.ReceiptId} -> Entities: {string.Join(", ", lastEntities)} | States: {string.Join(", ", lastStates)}");

                    foreach (var entry in ex.Entries)
                    {
                        switch (entry.State)
                        {
                            case EntityState.Modified:
                            case EntityState.Unchanged:
                                await entry.ReloadAsync(ct);
                                break;
                            default:
                                entry.State = EntityState.Detached;
                                break;
                        }
                    }
                    _db.ChangeTracker.Clear(); // بازخوانی برای تلاش بعدی

                    if (attempt == maxAttempts)
                        break;
                }
            }

            var entities = lastEntities.Any() ? $" (Entities: {string.Join(", ", lastEntities)})" : " (Entities: none)";
            var states = lastStates.Any() ? $" (States: {string.Join(", ", lastStates)})" : " (States: none)";
            var tracked = _db.ChangeTracker.Entries()
                .Select(e =>
                {
                    var pkValues = e.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .Select(p => $"{p.Metadata.Name}={p.CurrentValue}");
                    var pkText = string.Join(",", pkValues);
                    return $"{e.Entity.GetType().Name}:{e.State}[{pkText}]";
                });
            var trackedInfo = tracked.Any() ? $" (Tracked: {string.Join(" | ", tracked)})" : " (Tracked: none)";

            throw new InvalidOperationException(
                $"رکورد توسط کاربر دیگری تغییر یافته است. لطفا دوباره تلاش کنید.{entities}{states}{trackedInfo}",
                lastEx);
        });
    }
}
