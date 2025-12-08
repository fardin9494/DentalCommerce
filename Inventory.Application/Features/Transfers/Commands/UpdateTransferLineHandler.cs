using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class UpdateTransferLineHandler : IRequestHandler<UpdateTransferLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateTransferLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateTransferLineCommand req, CancellationToken ct)
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
                        }

                        // Clear segments before updating quantity
                        tr.ClearSegments(req.LineId);
                    }

                    // Update the quantity
                    line.UpdateQty(req.Qty);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    throw;
                }
            }
        });

        return Unit.Value;
    }
}

