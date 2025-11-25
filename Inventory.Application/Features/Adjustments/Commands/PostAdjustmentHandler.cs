namespace Inventory.Application.Features.Adjustments.Commands;

using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class PostAdjustmentHandler : IRequestHandler<PostAdjustmentCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public PostAdjustmentHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(PostAdjustmentCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var adj = await _db.Adjustments
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
            ?? throw new InvalidOperationException("Adjustment پیدا نشد.");

        if (adj.Status != AdjustmentStatus.Draft)
            throw new InvalidOperationException("فقط Draft قابل Post است.");

        var when = DateTime.SpecifyKind(req.WhenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        foreach (var l in adj.Lines)
        {
            var si = await _db.StockItems.FirstOrDefaultAsync(x =>
                x.WarehouseId == adj.WarehouseId &&
                x.ProductId == l.ProductId &&
                x.VariantId == l.VariantId &&
                x.LotNumber == l.LotNumber &&
                x.ExpiryDate == l.ExpiryDate, ct);

            if (si is null)
            {
                if (l.QtyDelta < 0)
                    throw new InvalidOperationException("برای کاهش، آیتم انبار باید وجود داشته باشد.");

                si = StockItem.Create(l.ProductId, l.VariantId, adj.WarehouseId, l.LotNumber, l.ExpiryDate);
                _db.StockItems.Add(si);
            }

            if (l.QtyDelta > 0) si.Increase(l.QtyDelta);
            else si.Decrease(Math.Abs(l.QtyDelta));

            // ✅ نوع تفکیک‌شده
            var moveType = l.QtyDelta > 0
                ? StockMovementType.AdjustmentPlus
                : StockMovementType.AdjustmentMinus;

            var led = StockLedgerEntry.Create(
                timestampUtc: when,
                productId: l.ProductId,
                variantId: l.VariantId,
                warehouseId: adj.WarehouseId,
                lotNumber: si.LotNumber,
                expiryDate: si.ExpiryDate,
                deltaQty: l.QtyDelta,   // + یا - همان‌طور که هست
                type: moveType,
                refDocType: "Adjustment",
                refDocId: adj.Id,
                unitCost: null,
                note: adj.Note
            );
            _db.StockLedger.Add(led);
        }

        adj.Post(when);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}
