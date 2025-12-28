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
        var strategy = _db.Database.CreateExecutionStrategy();
        const int maxAttempts = 5;

        await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var source = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == req.SourceStockItemId, ct)
                        ?? throw new InvalidOperationException("ردیف مبدا موجود نیست.");

                    if (source.ShelfId == req.TargetShelfId)
                        throw new InvalidOperationException("محل مقصد با مبدا یکسان است.");

                    if (req.Qty > source.OnHand)
                        throw new InvalidOperationException("مقدار درخواستی بیش از موجودی است.");

                    decimal movingAvailable = 0;
                    decimal movingBlocked = 0;

                    if (source.ShelfId.HasValue)
                    {
                        // انتقال بین قفسه‌ها - فقط کالاهای Available قابل انتقال هستند
                        if (req.Qty > source.Available)
                            throw new InvalidOperationException($"مقدار درخواستی ({req.Qty}) بیش از موجودی آزاد ({source.Available}) است.");
                        
                        movingAvailable = req.Qty;
                    }
                    else
                    {
                        // چیدن موجودی جدید (بدون قفسه و مسدود)
                        if (source.Blocked == 0)
                            throw new InvalidOperationException("فقط موجودی‌های مسدود (موجودی‌های جدید) قابل چیدن در قفسه هستند.");

                        // برای موجودی‌های جدید (Blocked و بدون قفسه)، همه موجودی Blocked است
                        if (req.Qty > source.Blocked)
                            throw new InvalidOperationException($"مقدار درخواستی ({req.Qty}) بیش از موجودی مسدود ({source.Blocked}) است.");
                        
                        // بررسی اینکه آیا رسید مربوطه تایید نهایی شده یا نه
                        // تا زمانی که رسید تایید نهایی نشده، امکان چیدن در قفسه وجود ندارد
                        try
                        {
                            var receiptExists = await _db.Receipts
                                .Include(r => r.Lines)
                                .Where(r => r.WarehouseId == source.WarehouseId &&
                                          r.Status == ReceiptStatus.Approved && // فقط رسیدهای تایید نهایی شده
                                          r.Lines.Any(l =>
                                              l.ProductId == source.ProductId &&
                                              l.VariantId == source.VariantId &&
                                              l.LotNumber == source.LotNumber &&
                                              l.ExpiryDate == source.ExpiryDate))
                                .AnyAsync(ct);
                            
                            if (!receiptExists)
                            {
                                // بررسی اینکه آیا رسید دریافت شده (اما تایید نهایی نشده) وجود دارد یا نه
                                var receiptReceived = await _db.Receipts
                                    .Include(r => r.Lines)
                                    .Where(r => r.WarehouseId == source.WarehouseId &&
                                              r.Status == ReceiptStatus.Received && // رسید دریافت شده
                                              r.Lines.Any(l =>
                                                  l.ProductId == source.ProductId &&
                                                  l.VariantId == source.VariantId &&
                                                  l.LotNumber == source.LotNumber &&
                                                  l.ExpiryDate == source.ExpiryDate))
                                    .FirstOrDefaultAsync(ct);
                                
                                if (receiptReceived != null)
                                {
                                    throw new InvalidOperationException(
                                        $"امکان چیدن در قفسه وجود ندارد. رسید مربوطه (شناسه: {receiptReceived.Id}) هنوز تایید نهایی نشده است. " +
                                        "لطفاً ابتدا رسید را تایید نهایی کنید.");
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        "امکان چیدن در قفسه وجود ندارد. رسید مربوطه باید ابتدا تایید نهایی شود.");
                                }
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // خطاهای InvalidOperationException را دوباره throw می‌کنیم
                            throw;
                        }
                        catch (Exception ex)
                        {
                            // خطاهای دیگر را با پیام مناسب throw می‌کنیم
                            throw new InvalidOperationException(
                                $"خطا در بررسی وضعیت رسید: {ex.Message}. امکان چیدن در قفسه وجود ندارد. رسید مربوطه باید ابتدا تایید نهایی شود.",
                                ex);
                        }
                        
                        // همه موجودی Blocked است، پس باید همه را Unblock کنیم
                        movingBlocked = req.Qty;
                    }

                    var dest = await _db.StockItems.FirstOrDefaultAsync(si =>
                        si.ProductId == source.ProductId &&
                        si.VariantId == source.VariantId &&
                        si.WarehouseId == source.WarehouseId &&
                        si.LotNumber == source.LotNumber &&
                        si.ExpiryDate == source.ExpiryDate &&
                        si.ShelfId == req.TargetShelfId, ct);

                    if (dest is null)
                    {
                        // استفاده از SKU موجود در StockItem مبدا (denormalized)
                        dest = StockItem.Create(
                            productId: source.ProductId,
                            variantId: source.VariantId,
                            warehouseId: source.WarehouseId,
                            sku: source.Sku, // استفاده از SKU موجود
                            lotNumber: source.LotNumber,
                            expiry: source.ExpiryDate,
                            shelfId: req.TargetShelfId
                        );
                        _db.StockItems.Add(dest);

                        var sourceCost = await _db.InventoryCosts.OrderByDescending(c => c.RecordedAt).FirstOrDefaultAsync(c => c.StockItemId == source.Id, ct);
                        if (sourceCost is not null)
                        {
                            _db.InventoryCosts.Add(InventoryCost.Create(dest.Id, sourceCost.Amount, sourceCost.Currency));
                        }
                    }

                    // دریافت نام قفسه‌ها برای ثبت در کاردکس
                    var sourceShelfName = source.ShelfId.HasValue
                        ? await _db.StockShelves
                            .Where(s => s.Id == source.ShelfId.Value)
                            .Select(s => s.Name)
                            .FirstOrDefaultAsync(ct) ?? "نامشخص"
                        : "بدون قفسه";
                    
                    var destShelfName = await _db.StockShelves
                        .Where(s => s.Id == req.TargetShelfId)
                        .Select(s => s.Name)
                        .FirstOrDefaultAsync(ct) ?? "نامشخص";

                    var totalQty = movingAvailable + movingBlocked;
                    var note = req.Note ?? (source.ShelfId.HasValue 
                        ? $"انتقال از قفسه {sourceShelfName} به قفسه {destShelfName}"
                        : $"چیدن در قفسه {destShelfName}");

                    if (movingAvailable > 0)
                    {
                        source.Decrease(movingAvailable);
                        dest.Increase(movingAvailable);
                        // در مقصد، چون در قفسه است، Available می‌شود (نیازی به Block نیست)
                    }

                    if (movingBlocked > 0)
                    {
                        // آزاد کردن از Blocked (فقط برای چیدن موجودی جدید)
                        source.Unblock(movingBlocked);
                        // کاهش از source
                        source.Decrease(movingBlocked);
                        // افزایش در dest
                        dest.Increase(movingBlocked);
                        // در مقصد، چون در قفسه است، Available می‌شود (نیازی به Block نیست)
                    }

                    await MoveSerialsAsync(source, dest, movingAvailable, movingBlocked, ct);


                    // ثبت در کاردکس: کاهش از قفسه مبدا
                    var sourceEntry = StockLedgerEntry.Create(
                        timestampUtc: DateTime.UtcNow,
                        productId: source.ProductId,
                        variantId: source.VariantId,
                        warehouseId: source.WarehouseId,
                        lotNumber: source.LotNumber,
                        expiryDate: source.ExpiryDate,
                        deltaQty: -totalQty,
                        type: StockMovementType.ShelfTransferOut,
                        refDocType: "StockMove",
                        refDocId: source.Id, // استفاده از source StockItemId به عنوان ref
                        unitCost: null,
                        note: note
                    );
                    _db.StockLedger.Add(sourceEntry);

                    // ثبت در کاردکس: افزایش در قفسه مقصد
                    var destEntry = StockLedgerEntry.Create(
                        timestampUtc: DateTime.UtcNow,
                        productId: dest.ProductId,
                        variantId: dest.VariantId,
                        warehouseId: dest.WarehouseId,
                        lotNumber: dest.LotNumber,
                        expiryDate: dest.ExpiryDate,
                        deltaQty: +totalQty,
                        type: StockMovementType.ShelfTransferIn,
                        refDocType: "StockMove",
                        refDocId: dest.Id, // استفاده از dest StockItemId به عنوان ref
                        unitCost: null,
                        note: note
                    );
                    _db.StockLedger.Add(destEntry);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (InvalidOperationException)
                {
                    // خطاهای InvalidOperationException را دوباره throw می‌کنیم (بدون retry)
                    await tx.RollbackAsync(ct);
                    throw;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    
                    // اگر آخرین attempt بود، خطا را throw می‌کنیم
                    if (attempt == maxAttempts)
                    {
                        throw new InvalidOperationException(
                            $"خطا در انتقال کالا به قفسه: {ex.Message}",
                            ex);
                    }
                    // در غیر این صورت retry می‌کنیم
                }
            }
        });

        return Unit.Value;
    }
    private async Task MoveSerialsAsync(
        StockItem source,
        StockItem dest,
        decimal movingAvailable,
        decimal movingBlocked,
        CancellationToken ct)
    {
        if (movingAvailable <= 0 && movingBlocked <= 0) return;

        var hasSerials = await _db.StockItemSerials.AnyAsync(s => s.StockItemId == source.Id, ct);
        if (!hasSerials) return;

        var availableQty = movingAvailable > 0 ? EnsureWholeQty(movingAvailable) : 0;
        var blockedQty = movingBlocked > 0 ? EnsureWholeQty(movingBlocked) : 0;
        var isShelved = dest.ShelfId.HasValue;

        if (availableQty > 0)
        {
            var availableSerials = await _db.StockItemSerials
                .Where(s => s.StockItemId == source.Id && s.Status == StockSerialStatus.Available)
                .OrderBy(s => s.SerialNumber)
                .Take(availableQty)
                .ToListAsync(ct);

            if (availableSerials.Count < availableQty)
                throw new InvalidOperationException("Not enough available serials to move.");

            foreach (var serial in availableSerials)
            {
                serial.MoveToStock(dest.Id, isShelved);
            }
        }

        if (blockedQty > 0)
        {
            var awaitingSerials = await _db.StockItemSerials
                .Where(s => s.StockItemId == source.Id && s.Status == StockSerialStatus.AwaitingShelving)
                .OrderBy(s => s.SerialNumber)
                .Take(blockedQty)
                .ToListAsync(ct);

            if (awaitingSerials.Count < blockedQty)
                throw new InvalidOperationException("Not enough awaiting shelving serials to move.");

            foreach (var serial in awaitingSerials)
            {
                serial.MoveToStock(dest.Id, isShelved);
            }
        }
    }

    private static int EnsureWholeQty(decimal qty)
    {
        var truncated = decimal.Truncate(qty);
        if (qty != truncated)
            throw new InvalidOperationException("Serialized stock moves require whole-number quantities.");
        return (int)truncated;
    }
}

