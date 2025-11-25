// src/Inventory/Inventory.Application/Features/Transfers/Commands/ReceiveTransferHandler.cs
using Inventory.Application.Features.Transfers.Commands;
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
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var tr = await _db.Transfers
            .Include(t => t.Lines)
                .ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
            ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");

        if (tr.Status is not (TransferStatus.Shipped or TransferStatus.PartiallyReceived))
            throw new InvalidOperationException("سند باید در وضعیت Shipped/PartiallyReceived باشد.");

        var when = DateTime.SpecifyKind(req.WhenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        // طرح دریافت: یا همه‌ی باقی‌مانده، یا لیست ورودی کاربر
        var plan = new List<(Guid SegmentId, Guid ProductId, Guid? VariantId, decimal Qty)>();

        if (req.Segments is null || req.Segments.Count == 0)
        {
            foreach (var l in tr.Lines)
                foreach (var s in l.Segments)
                {
                    var remain = s.RemainingToReceive;
                    if (remain > 0)
                        plan.Add((s.Id, l.ProductId, l.VariantId, remain));
                }
        }
        else
        {
            // اعتبارسنجی سگمنت‌ها و مقادیر
            var segIndex = tr.Lines.SelectMany(x => x.Segments).ToDictionary(s => s.Id);
            foreach (var dto in req.Segments)
            {
                if (!segIndex.TryGetValue(dto.SegmentId, out var seg))
                    throw new InvalidOperationException("Segment نامعتبر است.");

                if (dto.Qty <= 0 || dto.Qty > seg.RemainingToReceive)
                    throw new InvalidOperationException("مقدار دریافت نامعتبر است.");

                var line = tr.Lines.First(l => l.Segments.Contains(seg));
                plan.Add((seg.Id, line.ProductId, line.VariantId, dto.Qty));
            }
        }

        // اجرای دریافت برای هر مورد از طرح
        foreach (var item in plan)
        {
            var (segmentId, productId, variantId, qty) = item;

            // اطلاعات lot/expiry را از StockItemِ مبداِ سگمنت بخوان (در Ship تثبیت شده)
            var seg = tr.Lines.SelectMany(x => x.Segments).First(s => s.Id == segmentId);
            var srcSi = await _db.StockItems
                                 .AsNoTracking()
                                 .FirstAsync(x => x.Id == seg.StockItemId, ct);

            // در مقصد، آیتم انبار متناظر را پیدا/ایجاد کن (همان product/variant/lot/expiry)
            var destSi = await _db.StockItems.FirstOrDefaultAsync(x =>
                    x.WarehouseId == tr.DestinationWarehouseId &&
                    x.ProductId == productId &&
                    x.VariantId == variantId &&
                    x.LotNumber == srcSi.LotNumber &&
                    x.ExpiryDate == srcSi.ExpiryDate, ct);

            if (destSi is null)
            {
                destSi = StockItem.Create(
                    productId: productId,
                    variantId: variantId,
                    warehouseId: tr.DestinationWarehouseId,
                    lotNumber: srcSi.LotNumber,
                    expiry: srcSi.ExpiryDate
                );
                _db.StockItems.Add(destSi);
            }

            // افزایش موجودی مقصد
            destSi.Increase(qty);

            // ثبت در دفتر حرکات: TransferIn
            var led = StockLedgerEntry.Create(
                timestampUtc: when,
                productId: productId,
                variantId: variantId,
                warehouseId: tr.DestinationWarehouseId,
                lotNumber: destSi.LotNumber,
                expiryDate: destSi.ExpiryDate,
                deltaQty: qty,
                type: StockMovementType.TransferIn,
                refDocType: "Transfer",
                refDocId: tr.Id,
                unitCost: null,
                note: null
            );
            _db.StockLedger.Add(led);

            // بروزرسانی سگمنت از مسیر روت Aggregate (DDD-پسند)
            tr.ReceiveOnSegment(segmentId, qty);
        }

        // تعیین وضعیت نهایی/بینابینی
        tr.AfterReceiveEvaluateCompletion();

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}
