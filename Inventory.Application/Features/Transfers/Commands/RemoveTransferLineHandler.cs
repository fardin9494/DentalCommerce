using Inventory.Application.Features.Transfers.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class RemoveTransferLineHandler : IRequestHandler<RemoveTransferLineCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public RemoveTransferLineHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<Unit> Handle(RemoveTransferLineCommand req, CancellationToken ct)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _tx.ExecuteAsync(async ct =>
                {
                    var tr = await _db.Transfers
                        .Include(t => t.Lines)
                        .ThenInclude(l => l.Segments)
                        .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                        ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");

                    var line = tr.Lines.FirstOrDefault(l => l.Id == req.LineId)
                               ?? throw new InvalidOperationException("خط انتقال پیدا نشد.");

                    // Release all reserved stock items for this line's segments
                    foreach (var seg in line.Segments.ToList())
                    {
                        var stockItem = await _db.StockItems
                            .FirstOrDefaultAsync(si => si.Id == seg.StockItemId, ct)
                            ?? throw new InvalidOperationException($"StockItem با شناسه {seg.StockItemId} پیدا نشد.");

                        stockItem.Release(seg.Qty);
                        await TransferSerialsHelper.ReleaseReservedSerialsAsync(_db, seg.Id, ct);
                    }

                    // Clear segments and remove the line
                    tr.ClearSegments(req.LineId);
                    tr.RemoveLine(req.LineId);

                    await _db.SaveChangesAsync(ct);
                }, ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
                throw;
            }
        }

        return Unit.Value;
    }
}

