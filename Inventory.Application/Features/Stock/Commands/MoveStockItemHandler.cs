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

                    var qtyToMove = req.Qty;
                    var selectedSerials = NormalizeSerials(req.Serials, out var hasDuplicateSerials);
                    if (hasDuplicateSerials)
                        throw new InvalidOperationException("Duplicate serials are not allowed.");
                    if (selectedSerials.Count > 0)
                    {
                        var qtyInt = EnsureWholeQty(qtyToMove);
                        if (qtyInt != selectedSerials.Count)
                            throw new InvalidOperationException("Qty must match the number of serials.");
                    }

                    if (qtyToMove > source.OnHand)
                        throw new InvalidOperationException("مقدار درخواستی بیش از موجودی است.");

                    decimal movingAvailable = 0;
                    decimal movingBlocked = 0;

                    if (source.ShelfId.HasValue)
                    {
                        // انتقال بین قفسه‌ها - فقط کالاهای Available قابل انتقال هستند
                        if (qtyToMove > source.Available)
                            throw new InvalidOperationException($"مقدار درخواستی ({qtyToMove}) بیش از موجودی آزاد ({source.Available}) است.");
                        
                        movingAvailable = qtyToMove;
                    }
                    else
                    {
                        // چیدن موجودی جدید (بدون قفسه و مسدود)
                        if (source.Blocked == 0)
                            throw new InvalidOperationException("فقط موجودی‌های مسدود (موجودی‌های جدید) قابل چیدن در قفسه هستند.");

                        // برای موجودی‌های جدید (Blocked و بدون قفسه)، همه موجودی Blocked است
                        if (qtyToMove > source.Blocked)
                            throw new InvalidOperationException($"مقدار درخواستی ({qtyToMove}) بیش از موجودی مسدود ({source.Blocked}) است.");
                        
                        // Allow shelving transfer-in stock without receipt checks.
                        var hasTransferIn = await _db.StockLedger
                            .AsNoTracking()
                            .Where(l => l.MovementType == StockMovementType.TransferIn &&
                                        l.WarehouseId == source.WarehouseId &&
                                        l.ProductId == source.ProductId &&
                                        l.VariantId == source.VariantId &&
                                        l.LotNumber == source.LotNumber &&
                                        l.ExpiryDate == source.ExpiryDate)
                            .AnyAsync(ct);

                        if (!hasTransferIn)
                        {
                            try
                            {
                                var receiptExists = await _db.Receipts
                                    .Include(r => r.Lines)
                                    .Where(r => r.WarehouseId == source.WarehouseId &&
                                              r.Status == ReceiptStatus.Approved && // ??? ??????? ????? ????? ???
                                              r.Lines.Any(l =>
                                                  l.ProductId == source.ProductId &&
                                                  l.VariantId == source.VariantId &&
                                                  l.LotNumber == source.LotNumber &&
                                                  l.ExpiryDate == source.ExpiryDate))
                                    .AnyAsync(ct);

                                if (!receiptExists)
                                {
                                    // ????? ????? ??? ???? ?????? ??? (??? ????? ????? ????) ???? ???? ?? ??
                                    var receiptReceived = await _db.Receipts
                                        .Include(r => r.Lines)
                                        .Where(r => r.WarehouseId == source.WarehouseId &&
                                                  r.Status == ReceiptStatus.Received && // ???? ?????? ???
                                                  r.Lines.Any(l =>
                                                      l.ProductId == source.ProductId &&
                                                      l.VariantId == source.VariantId &&
                                                      l.LotNumber == source.LotNumber &&
                                                      l.ExpiryDate == source.ExpiryDate))
                                        .FirstOrDefaultAsync(ct);

                                    if (receiptReceived != null)
                                    {
                                        throw new InvalidOperationException(
                                            $"????? ???? ?? ???? ???? ?????. ???? ?????? (?????: {receiptReceived.Id}) ???? ????? ????? ???? ???. " +
                                            "????? ????? ???? ?? ????? ????? ????.");
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException(
                                            "????? ???? ?? ???? ???? ?????. ???? ?????? ???? ????? ????? ????? ???.");
                                    }
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // ?????? InvalidOperationException ?? ?????? throw ???????
                                throw;
                            }
                            catch (Exception ex)
                            {
                                // ?????? ???? ?? ?? ???? ????? throw ???????
                                throw new InvalidOperationException(
                                    $"??? ?? ????? ????? ????: {ex.Message}. ????? ???? ?? ???? ???? ?????. ???? ?????? ???? ????? ????? ????? ???.",
                                    ex);
                            }
                        }

                        movingBlocked = qtyToMove;
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
                    }

                    if (movingBlocked > 0)
                    {
                        // آزاد کردن از Blocked (فقط برای چیدن موجودی جدید)
                        source.Unblock(movingBlocked);
                        // کاهش از source
                        source.Decrease(movingBlocked);
                        // افزایش در dest
                        dest.Increase(movingBlocked);
                    }

                    await MoveSerialsAsync(source, dest, movingAvailable, movingBlocked, selectedSerials, ct);


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
        IReadOnlyList<string> selectedSerials,
        CancellationToken ct)
    {
        if (movingAvailable <= 0 && movingBlocked <= 0) return;

        var hasSerials = await _db.StockItemSerials.AnyAsync(s => s.StockItemId == source.Id, ct);
        if (!hasSerials)
        {
            if (selectedSerials.Count > 0)
                throw new InvalidOperationException("Serials were provided but the stock item has no serials.");
            return;
        }

        var isShelved = dest.ShelfId.HasValue;

        if (selectedSerials.Count > 0)
        {
            var serials = await _db.StockItemSerials
                .Where(s => s.StockItemId == source.Id && selectedSerials.Contains(s.SerialNumber))
                .ToListAsync(ct);

            if (serials.Count != selectedSerials.Count)
                throw new InvalidOperationException("Selected serials were not found in source stock.");

            if (movingAvailable > 0 && serials.Any(s => s.Status != StockSerialStatus.Available))
                throw new InvalidOperationException("Selected serials are not available.");

            if (movingBlocked > 0 && serials.Any(s => s.Status != StockSerialStatus.AwaitingShelving))
                throw new InvalidOperationException("Selected serials are not awaiting shelving.");

            foreach (var serial in serials)
            {
                serial.MoveToStock(dest.Id, isShelved);
            }

            return;
        }

        var availableQty = movingAvailable > 0 ? EnsureWholeQty(movingAvailable) : 0;
        var blockedQty = movingBlocked > 0 ? EnsureWholeQty(movingBlocked) : 0;

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

    private static IReadOnlyList<string> NormalizeSerials(
        IReadOnlyList<string>? serials,
        out bool hasDuplicates)
    {
        hasDuplicates = false;
        if (serials is null || serials.Count == 0) return Array.Empty<string>();

        var cleaned = new List<string>(serials.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var serial in serials)
        {
            if (string.IsNullOrWhiteSpace(serial)) continue;
            var trimmed = serial.Trim();
            if (!seen.Add(trimmed))
            {
                hasDuplicates = true;
                continue;
            }
            cleaned.Add(trimmed);
        }

        return cleaned;
    }
}




