using Inventory.Application.Features.Transfers.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class UpdateTransferLineHandler : IRequestHandler<UpdateTransferLineCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public UpdateTransferLineHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<Unit> Handle(UpdateTransferLineCommand req, CancellationToken ct)
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
                               ?? throw new InvalidOperationException("خط سند انتقال پیدا نشد.");

                    // If quantity is being changed and there are existing segments, release them first
                    if (line.Segments.Count > 0 && line.RequestedQty != req.Qty)
                    {
                        // Release all reserved stock items for existing segments
                        foreach (var seg in line.Segments.ToList())
                        {
                            var stockItem = await _db.StockItems
                                .FirstOrDefaultAsync(si => si.Id == seg.StockItemId, ct)
                                ?? throw new InvalidOperationException($"StockItem با شناسه {seg.StockItemId} پیدا نشد.");

                            stockItem.Release(seg.Qty);
                            await TransferSerialsHelper.ReleaseReservedSerialsAsync(_db, seg.Id, ct);
                        }

                        // Clear segments before updating quantity
                        tr.ClearSegments(req.LineId);
                    }

                    // Update the quantity
                    line.UpdateQty(req.Qty);

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

