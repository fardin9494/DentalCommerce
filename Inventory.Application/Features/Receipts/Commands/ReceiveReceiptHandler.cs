using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class ReceiveReceiptHandler : IRequestHandler<ReceiveReceiptCommand, Unit>
{
    private readonly InventoryDbContext _db;

    public ReceiveReceiptHandler(InventoryDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(ReceiveReceiptCommand req, CancellationToken ct)
    {
        // استفاده از تراکنش برای تضمین یکپارچگی (ورود کالا + ثبت قیمت + سند حسابداری)
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1. دریافت سند رسید
        var rec = await _db.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct);

        if (rec is null)
            throw new InvalidOperationException("رسید مورد نظر یافت نشد.");

        var when = req.WhenUtc ?? DateTime.UtcNow;

        // 2. تغییر وضعیت سند در دامین (Draft -> Received)
        // این متد وضعیت را تغییر داده و زمان ReceiveAt را ست می‌کند
        rec.Receive(when);

        // 3. اعمال تغییرات روی موجودی (StockItems)
        foreach (var l in rec.Lines.OrderBy(x => x.LineNo))
        {
            // جستجو برای یافتن آیتم موجود (آیا قبلاً از این کالا با همین لات و انقضا در شلف ورودی داشته‌ایم؟)
            // نکته: فعلاً ShelfId را null در نظر می‌گیریم چون هنوز سیستم Put-away نداریم.
            var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                    si.ProductId == l.ProductId &&
                    si.VariantId == l.VariantId &&
                    si.WarehouseId == rec.WarehouseId &&
                    si.LotNumber == l.LotNumber &&
                    si.ExpiryDate == l.ExpiryDate &&
                    si.ShelfId == null, // جستجو در کالاهای بدون شلف (پای سکو)
                ct);

            // اگر موجودی برای این مشخصات وجود نداشت، رکورد جدید می‌سازیم
            if (stock is null)
            {
                stock = StockItem.Create(
                    productId: l.ProductId,
                    variantId: l.VariantId,
                    warehouseId: rec.WarehouseId,
                    lotNumber: l.LotNumber,
                    expiry: l.ExpiryDate,
                    shelfId: null // شلف پیش‌فرض نال
                );

                _db.StockItems.Add(stock);

                // --- تغییر جدید: ثبت قیمت تمام شده (Cost) ---
                // فقط برای آیتم‌های جدید یا بچ‌های جدید قیمت خرید ثبت می‌شود
                if (l.UnitCost.HasValue)
                {
                    // ارز را می‌توان از هدر رسید یا تنظیمات گرفت، اینجا فرض بر ریال (IRR) است
                    var cost = InventoryCost.Create(stock.Id, l.UnitCost.Value, "IRR");
                    _db.InventoryCosts.Add(cost);
                }
            }

            // الف) افزایش موجودی فیزیکی (OnHand افزایش می‌یابد)
            stock.Increase(l.Qty);

            // ب) اعمال قرنطینه (Available کاهش می‌یابد، Blocked افزایش می‌یابد)
            // دلیل: کالا وارد شده اما هنوز توسط مدیر تایید (Approve) نشده است.
            stock.Block(l.Qty, "Quarantine - Waiting for Approval");

            // 4. ثبت در کاردکس کالا (Ledger)
            // کاردکس نشان‌دهنده ورود فیزیکی بار است، مستقل از اینکه قرنطینه است یا نه.
            var entry = StockLedgerEntry.Create(
                timestampUtc: when,
                productId: l.ProductId,
                variantId: l.VariantId,
                warehouseId: rec.WarehouseId,
                lotNumber: l.LotNumber,
                expiryDate: l.ExpiryDate,
                deltaQty: +l.Qty, // مثبت: ورود به انبار
                type: StockMovementType.Receipt,
                refDocType: nameof(Receipt),
                refDocId: rec.Id,
                unitCost: l.UnitCost,
                note: $"Received (Quarantine) - {rec.ExternalRef}"
            );

            _db.StockLedger.Add(entry);
        }

        // ذخیره تمام تغییرات
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Unit.Value;
    }
}