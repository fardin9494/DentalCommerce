using Inventory.Application.Features.Transfers.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class CancelTransferHandler : IRequestHandler<CancelTransferCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public CancelTransferHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<Unit> Handle(CancelTransferCommand req, CancellationToken ct)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _tx.ExecuteAsync(async ct =>
                {
                    var tr = await _db.Transfers
                                 .AsSplitQuery()
                                 .Include(t => t.Lines).ThenInclude(l => l.Segments)
                                 .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                             ?? throw new InvalidOperationException("انتقال پیدا نشد.");

                    foreach (var l in tr.Lines.ToList())
                    {
                        foreach (var s in l.Segments.ToList())
                        {
                            var si = await _db.StockItems.FirstAsync(x => x.Id == s.StockItemId, ct);
                            si.Release(s.Qty);
                            await TransferSerialsHelper.ReleaseReservedSerialsAsync(_db, s.Id, ct);
                        }
                        tr.ClearSegments(l.Id);
                    }

                    tr.Cancel();

                    await _db.SaveChangesAsync(ct);
                }, ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
        }

        return Unit.Value;
    }
}
