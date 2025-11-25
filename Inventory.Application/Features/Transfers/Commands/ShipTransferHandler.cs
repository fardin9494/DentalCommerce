using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class ShipTransferHandler : IRequestHandler<ShipTransferCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public ShipTransferHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ShipTransferCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers
            .Include(t => t.Lines)
            .ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct);

        if (tr is null)
            throw new InvalidOperationException("سند انتقال یافت نشد.");

        if (tr.Status != TransferStatus.Draft)
            throw new InvalidOperationException("فقط سند پیش‌نویس قابل ارسال است.");

        if (tr.Lines.Count == 0)
            throw new InvalidOperationException("انتقال بدون آیتم قابل ارسال نیست.");

        if (tr.Lines.Any(l => l.RemainingQty > 0))
            throw new InvalidOperationException("همه خطوط باید کامل سگمنت‌بندی شوند.");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    // از انبار مبدا کم کن (برای هر سگمنت)
                    foreach (var line in tr.Lines)
                    {
                        foreach (var seg in line.Segments)
                        {
                            var srcItem = await _db.StockItems
                                .FirstOrDefaultAsync(si => si.Id == seg.StockItemId, ct)
                                ?? throw new InvalidOperationException("StockItem مبدا یافت نشد.");

                            // کم کردن از مبدا
                            srcItem.Decrease(seg.Qty);

                            var led = StockLedgerEntry.Create(
                                timestampUtc: DateTime.UtcNow,
                                productId: srcItem.ProductId,
                                variantId: srcItem.VariantId,
                                warehouseId: srcItem.WarehouseId, // مبدا
                                lotNumber: srcItem.LotNumber,
                                expiryDate: srcItem.ExpiryDate,
                                deltaQty: -seg.Qty,
                                type: StockMovementType.TransferOut,
                                refDocType: nameof(Transfer),
                                refDocId: tr.Id,
                                unitCost: null,
                                note: tr.ExternalRef
                            );
                            _db.StockLedger.Add(led);
                        }
                    }

                    tr.Ship(req.WhenUtc);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();

                    tr = await _db.Transfers
                        .Include(t => t.Lines)
                        .ThenInclude(l => l.Segments)
                        .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                        ?? throw new InvalidOperationException("سند انتقال در تلاش مجدد یافت نشد.");

                    if (attempt == maxAttempts)
                        throw;
                }
            }
        });

        return Unit.Value;
    }
}
