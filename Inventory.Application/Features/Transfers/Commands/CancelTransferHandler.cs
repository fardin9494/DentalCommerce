using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class CancelTransferHandler : IRequestHandler<CancelTransferCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public CancelTransferHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(CancelTransferCommand req, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        const int maxAttempts = 5;

        await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var tr = await _db.Transfers
                                 .Include(t => t.Lines).ThenInclude(l => l.Segments)
                                 .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                             ?? throw new InvalidOperationException("انتقال پیدا نشد.");

                    foreach (var l in tr.Lines.ToList())
                    {
                        foreach (var s in l.Segments.ToList())
                        {
                            var si = await _db.StockItems.FirstAsync(x => x.Id == s.StockItemId, ct);
                            si.Release(s.Qty);
                        }
                        tr.ClearSegments(l.Id);
                    }

                    tr.Cancel();

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
            }
        });

        return Unit.Value;
    }
}