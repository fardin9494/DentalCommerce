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
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var tr = await _db.Transfers
                     .Include(t => t.Lines).ThenInclude(l => l.Segments)
                     .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                 ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");

        // آزاد کردن رزروها
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
        return Unit.Value;
    }
}