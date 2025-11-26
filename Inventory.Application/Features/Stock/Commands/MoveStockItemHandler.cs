using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Commands;

public sealed class MoveStockItemHandler : IRequestHandler<MoveStockItemCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public MoveStockItemHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(MoveStockItemCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1. یافتن رکورد مبدا
        var sourceStock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == req.SourceStockItemId, ct);
        if (sourceStock is null) throw new InvalidOperationException("رکورد موجودی مبدا یافت نشد.");

        if (sourceStock.ShelfId == req.TargetShelfId)
            throw new InvalidOperationException("مبدا و مقصد نمی‌توانند یکسان باشند.");

        // 2. واکشی قیمت تمام شده (Cost) مبدا
        // این قیمت برای ثبت در کاردکس و انتقال به مقصد ضروری است
        var sourceCost = await _db.InventoryCosts
            .OrderByDescending(c => c.RecordedAt) // گرفتن آخرین قیمت ثبت شده
            .FirstOrDefaultAsync(c => c.StockItemId == sourceStock.Id, ct);

        decimal? unitCostValue = sourceCost?.Amount;
        string currency = sourceCost?.Currency ?? "IRR";

        // 3. یافتن یا ایجاد مقصد (Destination StockItem)
        var destStock = await _db.StockItems.FirstOrDefaultAsync(si =>
            si.ProductId == sourceStock.ProductId &&
            si.VariantId == sourceStock.VariantId &&
            si.WarehouseId == sourceStock.WarehouseId &&
            si.LotNumber == sourceStock.LotNumber &&
            si.ExpiryDate == sourceStock.ExpiryDate &&
            si.ShelfId == req.TargetShelfId,
            ct);

        if (destStock is null)
        {
            // ایجاد رکورد جدید در شلف مقصد
            destStock = StockItem.Create(
                sourceStock.ProductId,
                sourceStock.VariantId,
                sourceStock.WarehouseId,
                sourceStock.LotNumber,
                sourceStock.ExpiryDate,
                req.TargetShelfId
            );
            _db.StockItems.Add(destStock);

            // *** مهم: کپی کردن قیمت خرید برای آیتم جدید در شلف جدید ***
            if (unitCostValue.HasValue)
            {
                var newCost = InventoryCost.Create(destStock.Id, unitCostValue.Value, currency);
                _db.InventoryCosts.Add(newCost);
            }
        }
        else
        {
            // اگر آیتم در مقصد وجود دارد، چک می‌کنیم که قیمت داشته باشد.
            // اگر نداشت (به هر دلیلی)، می‌توانیم قیمت مبدا را برایش ست کنیم (اختیاری)
            // اما چون فرض بر این است که (Product+Lot) قیمت یکسانی دارد، معمولاً نیاز به آپدیت نیست
            // مگر اینکه بخواهید میانگین بگیرید که پیچیده می‌شود.
        }

        // 4. انتقال موجودی
        sourceStock.Decrease(req.Qty);
        destStock.Increase(req.Qty);

        // 5. ثبت در Ledger با قیمت (UnitCost)
        var timestamp = DateTime.UtcNow;

        // خروج از شلف قدیم
        _db.StockLedger.Add(StockLedgerEntry.Create(
            timestampUtc: timestamp,
            productId: sourceStock.ProductId,
            variantId: sourceStock.VariantId,
            warehouseId: sourceStock.WarehouseId,
            lotNumber: sourceStock.LotNumber,
            expiryDate: sourceStock.ExpiryDate,
            deltaQty: -req.Qty, // منفی
            type: StockMovementType.AdjustmentMinus, // یا تایپ اختصاصی InternalTransferOut
            refDocType: "InternalMove",
            refDocId: sourceStock.Id,
            unitCost: unitCostValue, // <--- مقداردهی شد
            note: $"Move Out to Shelf {req.TargetShelfId}"
        ));

        // ورود به شلف جدید
        _db.StockLedger.Add(StockLedgerEntry.Create(
            timestampUtc: timestamp,
            productId: destStock.ProductId,
            variantId: destStock.VariantId,
            warehouseId: destStock.WarehouseId,
            lotNumber: destStock.LotNumber,
            expiryDate: destStock.ExpiryDate,
            deltaQty: +req.Qty, // مثبت
            type: StockMovementType.AdjustmentPlus, // یا تایپ اختصاصی InternalTransferIn
            refDocType: "InternalMove",
            refDocId: destStock.Id,
            unitCost: unitCostValue, // <--- مقداردهی شد
            note: $"Move In from Shelf {sourceStock.ShelfId}" // اینجا ShelfId ممکن است نال باشد که در استرینگ هندل می‌شود
        ));

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Unit.Value;
    }
}