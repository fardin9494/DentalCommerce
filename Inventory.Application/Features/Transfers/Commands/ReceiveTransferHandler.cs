using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class ReceiveTransferHandler : IRequestHandler<ReceiveTransferCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public ReceiveTransferHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ReceiveTransferCommand req, CancellationToken ct)
    {
        // Transfer + Segments
        var tr = await _db.Transfers
            .Include(t => t.Lines)
            .ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct);

        if (tr is null)
            throw new InvalidOperationException("سند انتقال یافت نشد.");

        if (tr.Status is not (TransferStatus.Shipped or TransferStatus.PartiallyReceived))
            throw new InvalidOperationException("در این وضعیت امکان ثبت دریافت نیست.");

        // سگمنتی که قرار است در مقصد دریافت شود
        var segment = tr.Lines.SelectMany(l => l.Segments).FirstOrDefault(s => s.Id == req.SegmentId)
                      ?? throw new InvalidOperationException("سگمنت موردنظر یافت نشد.");

        if (req.Qty <= 0) throw new ArgumentOutOfRangeException(nameof(req.Qty));
        if (req.Qty > segment.RemainingToReceive)
            throw new InvalidOperationException("بیش از مقدار مجاز دریافت درخواست شده است.");

        // برای ساخت StockItem مقصد باید از StockItem مبدا، ویژگی‌های lot/expiry را بخوانیم
        var srcItem = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == segment.StockItemId, ct)
                      ?? throw new InvalidOperationException("StockItem مبدا برای سگمنت یافت نشد.");

        var destWarehouseId = tr.DestinationWarehouseId;

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    // StockItem مقصد را پیدا یا ایجاد کن
                    var destItem = await _db.StockItems.FirstOrDefaultAsync(si =>
                            si.ProductId == srcItem.ProductId &&
                            si.VariantId == srcItem.VariantId &&
                            si.WarehouseId == destWarehouseId &&
                            si.LotNumber == srcItem.LotNumber &&
                            si.ExpiryDate == srcItem.ExpiryDate,
                        ct);

                    if (destItem is null)
                    {
                        // استفاده از SKU موجود در StockItem مبدا (denormalized)
                        destItem = StockItem.Create(
                            productId: srcItem.ProductId,
                            variantId: srcItem.VariantId,
                            warehouseId: destWarehouseId,
                            sku: srcItem.Sku, // استفاده از SKU موجود
                            lotNumber: srcItem.LotNumber,
                            expiry: srcItem.ExpiryDate
                        );
                        _db.StockItems.Add(destItem);
                    }

                    // افزایش موجودی مقصد
                    destItem.Increase(req.Qty);

                    // ثبت در دفتر انبار
                    var led = StockLedgerEntry.Create(
                        timestampUtc: DateTime.UtcNow,
                        productId: destItem.ProductId,
                        variantId: destItem.VariantId,
                        warehouseId: destItem.WarehouseId, // مقصد
                        lotNumber: destItem.LotNumber,
                        expiryDate: destItem.ExpiryDate,
                        deltaQty: +req.Qty,
                        type: StockMovementType.TransferIn,
                        refDocType: nameof(Transfer),
                        refDocId: tr.Id,
                        unitCost: null,
                        note: tr.ExternalRef
                    );
                    _db.StockLedger.Add(led);

                    // به‌روز کردن سگمنت در دامنه
                    tr.ReceiveOnSegment(segment.Id, req.Qty);
                    tr.AfterReceiveEvaluateCompletion();

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();

                    // re-load برای تلاش بعدی
                    tr = await _db.Transfers
                        .Include(t => t.Lines)
                        .ThenInclude(l => l.Segments)
                        .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                        ?? throw new InvalidOperationException("سند انتقال در تلاش مجدد یافت نشد.");

                    segment = tr.Lines.SelectMany(l => l.Segments)
                               .FirstOrDefault(s => s.Id == req.SegmentId)
                               ?? throw new InvalidOperationException("سگمنت در تلاش مجدد یافت نشد.");

                    srcItem = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == segment.StockItemId, ct)
                              ?? throw new InvalidOperationException("StockItem مبدا در تلاش مجدد یافت نشد.");

                    if (attempt == maxAttempts)
                        throw;
                }
            }
        });

        return Unit.Value;
    }
}
