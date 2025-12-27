using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Application.Features.Transfers.Serials;
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
                    // Reload transfer and segment in case of retry
                    if (attempt > 1)
                    {
                        tr = await _db.Transfers
                            .Include(t => t.Lines)
                            .ThenInclude(l => l.Segments)
                            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                            ?? throw new InvalidOperationException($"سند انتقال {req.TransferId} در تلاش مجدد {attempt} یافت نشد.");

                        if (tr.Status is not (TransferStatus.Shipped or TransferStatus.PartiallyReceived))
                            throw new InvalidOperationException(
                                $"وضعیت سند انتقال {req.TransferId} در تلاش مجدد {attempt} تغییر کرده است. " +
                                $"وضعیت فعلی: {tr.Status}"
                            );

                        segment = tr.Lines.SelectMany(l => l.Segments)
                                   .FirstOrDefault(s => s.Id == req.SegmentId)
                                   ?? throw new InvalidOperationException($"سگمنت {req.SegmentId} در تلاش مجدد {attempt} یافت نشد.");

                        if (req.Qty > segment.RemainingToReceive)
                            throw new InvalidOperationException(
                                $"مقدار درخواستی {req.Qty} بیش از مقدار باقی‌مانده {segment.RemainingToReceive} است. " +
                                $"(SegmentId: {req.SegmentId}, TransferId: {req.TransferId})"
                            );

                        srcItem = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == segment.StockItemId, ct)
                                  ?? throw new InvalidOperationException($"StockItem مبدا {segment.StockItemId} در تلاش مجدد {attempt} یافت نشد.");
                    }

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
                        try
                        {
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
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"خطا در ایجاد StockItem مقصد برای محصول {srcItem.ProductId} (SKU: {srcItem.Sku}): {ex.Message}. " +
                                $"(TransferId: {tr.Id}, SegmentId: {segment.Id}, WarehouseId: {destWarehouseId})",
                                ex
                            );
                        }
                    }

                    try
                    {
                        // افزایش موجودی مقصد
                        destItem.Increase(req.Qty);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw new ArgumentException(
                            $"مقدار نامعتبر برای افزایش موجودی: {req.Qty}. " +
                            $"(TransferId: {tr.Id}, SegmentId: {segment.Id}, StockItemId: {destItem.Id})",
                            ex
                        );
                    }

                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"خطا در ایجاد رکورد کاردکس برای StockItem مقصد {destItem.Id} (SKU: {destItem.Sku}): {ex.Message}. " +
                            $"(TransferId: {tr.Id}, SegmentId: {segment.Id})",
                            ex
                        );
                    }

                    var isShelved = destItem.ShelfId != null;
                    await TransferSerialsHelper.ReceiveSerialsAsync(_db, segment.Id, destItem.Id, req.Qty, isShelved, ct);

                    try
                    {
                        // به‌روز کردن سگمنت در دامنه
                        tr.ReceiveOnSegment(segment.Id, req.Qty);
                        tr.AfterReceiveEvaluateCompletion();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException(
                            $"خطا در ثبت دریافت سگمنت {segment.Id}: {ex.Message}. " +
                            $"(TransferId: {tr.Id}, SegmentId: {segment.Id}, Qty: {req.Qty}, " +
                            $"RemainingToReceive: {segment.RemainingToReceive})",
                            ex
                        );
                    }

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    // Continue to next attempt - reload will happen at the beginning of the loop
                }
                catch (InvalidOperationException)
                {
                    // Re-throw business logic errors as-is
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    throw;
                }
                catch (ArgumentException)
                {
                    // Re-throw argument errors as-is
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    throw;
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    // Log and retry for other exceptions
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    // Continue to next attempt
                }
                catch (Exception ex)
                {
                    // Last attempt failed, wrap and throw
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    throw new InvalidOperationException(
                        $"خطا در ثبت دریافت سند انتقال {req.TransferId} برای سگمنت {req.SegmentId} پس از {maxAttempts} تلاش: {ex.Message}",
                        ex
                    );
                }
            }
        });

        return Unit.Value;
    }
}

