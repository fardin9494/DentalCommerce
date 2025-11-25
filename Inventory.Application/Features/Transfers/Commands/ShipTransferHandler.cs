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
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var tr = await _db.Transfers
            .Include(t => t.Lines).ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
            ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");

        // اعتبارسنجی
        if (tr.Lines.Count == 0) throw new InvalidOperationException("سند انتقال بدون آیتم قابل ارسال نیست.");
        if (tr.Lines.Any(l => l.RemainingQty > 0))
            throw new InvalidOperationException("ابتدا همه خطوط را تخصیص دهید.");

        var when = DateTime.SpecifyKind(req.WhenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        foreach (var l in tr.Lines)
        {
            foreach (var s in l.Segments)
            {
                var si = await _db.StockItems.FirstAsync(x => x.Id == s.StockItemId, ct);

                // آزادسازی رزرو و کاهش موجودی
                si.Release(s.Qty);
                si.Decrease(s.Qty);

                // Ledger (TransferOut)
                var led = StockLedgerEntry.Create(
                    timestampUtc: when,
                    productId: l.ProductId,
                    variantId: l.VariantId,
                    warehouseId: tr.SourceWarehouseId,
                    lotNumber: si.LotNumber,
                    expiryDate: si.ExpiryDate,
                    deltaQty: -s.Qty,
                    type: StockMovementType.TransferOut,
                    refDocType: "Transfer",
                    refDocId: tr.Id,
                    unitCost: null,
                    note: null
                );
                _db.StockLedger.Add(led);
            }
        }

        tr.Ship(when);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}
