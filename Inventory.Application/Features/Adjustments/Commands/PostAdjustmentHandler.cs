using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed class PostAdjustmentHandler : IRequestHandler<PostAdjustmentCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public PostAdjustmentHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(PostAdjustmentCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct);

        if (adj is null)
            throw new InvalidOperationException("سند اصلاح موجودی یافت نشد.");

        if (adj.Status != AdjustmentStatus.Draft)
            throw new InvalidOperationException("فقط سند پیش‌نویس قابل ثبت است.");

        if (adj.Lines.Count == 0)
            throw new InvalidOperationException("سند بدون آیتم قابل ثبت نیست.");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    foreach (var l in adj.Lines)
                    {
                        // پیدا/ایجاد StockItem
                        var si = await _db.StockItems.FirstOrDefaultAsync(x =>
                                x.ProductId == l.ProductId &&
                                x.VariantId == l.VariantId &&
                                x.WarehouseId == adj.WarehouseId &&
                                x.LotNumber == l.LotNumber &&
                                x.ExpiryDate == l.ExpiryDate,
                            ct);

                        if (si is null)
                        {
                            // TODO: دریافت SKU از Catalog Service (فعلاً placeholder)
                            // در آینده باید از ICatalogProductService.GetSkuAsync استفاده شود
                            var sku = $"PROD-{l.ProductId}"; // Placeholder - باید از Catalog دریافت شود
                            
                            si = StockItem.Create(
                                productId: l.ProductId,
                                variantId: l.VariantId,
                                warehouseId: adj.WarehouseId,
                                sku: sku,
                                lotNumber: l.LotNumber,
                                expiry: l.ExpiryDate
                            );
                            _db.StockItems.Add(si);
                        }

                        // اعمال تغییر مقدار
                        if (l.QtyDelta > 0)
                            si.Increase(l.QtyDelta);
                        else
                            si.Decrease(-l.QtyDelta);

                        var mtype = l.QtyDelta > 0 ? StockMovementType.AdjustmentPlus : StockMovementType.AdjustmentMinus;

                        var ledger = StockLedgerEntry.Create(
                            timestampUtc: DateTime.UtcNow,
                            productId: si.ProductId,
                            variantId: si.VariantId,
                            warehouseId: si.WarehouseId,
                            lotNumber: si.LotNumber,
                            expiryDate: si.ExpiryDate,
                            deltaQty: l.QtyDelta,
                            type: mtype,
                            refDocType: nameof(Adjustment),
                            refDocId: adj.Id,
                            unitCost: null,
                            note: adj.Note
                        );
                        _db.StockLedger.Add(ledger);
                    }

                    adj.Post(req.WhenUtc);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();

                    adj = await _db.Adjustments
                        .Include(a => a.Lines)
                        .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                        ?? throw new InvalidOperationException("سند اصلاح در تلاش مجدد یافت نشد.");

                    if (attempt == maxAttempts)
                        throw;
                }
            }
        });

        return Unit.Value;
    }
}
