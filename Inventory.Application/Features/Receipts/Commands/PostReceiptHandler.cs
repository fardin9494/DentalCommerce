using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class PostReceiptHandler : IRequestHandler<PostReceiptCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public PostReceiptHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(PostReceiptCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var rec = await _db.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
            ?? throw new InvalidOperationException("رسید پیدا نشد.");

        if (rec.Status != ReceiptStatus.Draft)
            throw new InvalidOperationException("فقط رسید پیش‌نویس قابل پست است.");
        if (rec.Lines.Count == 0)
            throw new InvalidOperationException("رسید بدون آیتم قابل پست نیست.");

        var when = req.WhenUtc ?? DateTime.UtcNow;

        foreach (var l in rec.Lines)
        {
            // StockItem: پیدا کن یا بساز
            var item = await _db.StockItems.FirstOrDefaultAsync(si =>
                    si.ProductId == l.ProductId &&
                    si.VariantId == l.VariantId &&
                    si.WarehouseId == rec.WarehouseId &&
                    si.LotNumber == l.LotNumber &&
                    si.ExpiryDate == l.ExpiryDate, ct);

            if (item is null)
            {
                item = StockItem.Create(l.ProductId, l.VariantId, rec.WarehouseId, l.LotNumber, l.ExpiryDate);
                _db.StockItems.Add(item);
            }

            item.Increase(l.Qty);

            // Ledger
            var le = StockLedgerEntry.Create(
                timestampUtc: when,
                productId: l.ProductId,
                variantId: l.VariantId,
                warehouseId: rec.WarehouseId,
                lotNumber: l.LotNumber,
                expiryDate: l.ExpiryDate,
                deltaQty: +l.Qty,
                type: StockMovementType.Receipt,
                refDocType: "Receipt",
                refDocId: rec.Id,
                unitCost: l.UnitCost
            );
            _db.StockLedger.Add(le);
        }

        rec.Post(when);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}
