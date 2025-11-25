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
            .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct);

        if (rec is null) throw new InvalidOperationException("رسید یافت نشد.");

        // پست کردن و اعمال روی انبار
        var when = req.WhenUtc ?? DateTime.UtcNow;

        foreach (var l in rec.Lines.OrderBy(x => x.LineNo))
        {
            // Upsert StockItem
            var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                    si.ProductId == l.ProductId &&
                    si.VariantId == l.VariantId &&
                    si.WarehouseId == rec.WarehouseId &&
                    si.LotNumber == l.LotNumber &&
                    si.ExpiryDate == l.ExpiryDate,
                ct);

            if (stock is null)
            {
                stock = StockItem.Create(l.ProductId, l.VariantId, rec.WarehouseId, l.LotNumber, l.ExpiryDate);
                _db.StockItems.Add(stock);
            }

            // افزایش موجودی (RowVersion روی StockItem کنترل همزمانی رو انجام می‌ده)
            stock.Increase(l.Qty);

            // Ledger
            var entry = StockLedgerEntry.Create(
                timestampUtc: when,
                productId: l.ProductId,
                variantId: l.VariantId,
                warehouseId: rec.WarehouseId,
                lotNumber: l.LotNumber,
                expiryDate: l.ExpiryDate,
                deltaQty: +l.Qty,
                type: StockMovementType.Receipt,
                refDocType: nameof(Receipt),
                refDocId: rec.Id,
                unitCost: l.UnitCost,
                note: rec.ExternalRef
            );
            _db.StockLedger.Add(entry);
        }

        rec.Post(when);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}
