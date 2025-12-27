using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Application.Features.Transfers.Serials;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class ShipTransferHandler : IRequestHandler<ShipTransferCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public ShipTransferHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ShipTransferCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers
            .Include(t => t.Lines)
            .ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct);

        if (tr is null)
            throw new InvalidOperationException("سند انتقال یافت نشد.");

        if (tr.Status != TransferStatus.Draft)
            throw new InvalidOperationException("فقط سند پیش‌نویس قابل ارسال است.");

        if (tr.Lines.Count == 0)
            throw new InvalidOperationException("انتقال بدون آیتم قابل ارسال نیست.");

        if (tr.Lines.Any(l => l.RemainingQty > 0))
            throw new InvalidOperationException("همه خطوط باید کامل سگمنت‌بندی شوند.");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    // از انبار مبدا کم کن (برای هر سگمنت)
                    foreach (var line in tr.Lines)
                    {
                        foreach (var seg in line.Segments)
                        {
                            var srcItem = await _db.StockItems
                                .FirstOrDefaultAsync(si => si.Id == seg.StockItemId, ct);

                            if (srcItem is null)
                            {
                                throw new InvalidOperationException(
                                    $"StockItem با شناسه {seg.StockItemId} برای سگمنت {seg.Id} یافت نشد. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id})"
                                );
                            }

                            // بررسی اینکه موجودی رزرو شده کافی است
                            if (seg.Qty > srcItem.Reserved)
                            {
                                throw new InvalidOperationException(
                                    $"موجودی رزرو شده کافی نیست برای ارسال در StockItem {srcItem.Id}. " +
                                    $"SKU: {srcItem.Sku}, " +
                                    $"موجودی رزرو شده: {srcItem.Reserved}, " +
                                    $"مقدار درخواستی: {seg.Qty}, " +
                                    $"مجموع موجودی: {srcItem.OnHand}, " +
                                    $"موجودی آزاد: {srcItem.Available}, " +
                                    $"مسدود شده: {srcItem.Blocked}. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id}, " +
                                    $"ProductId: {srcItem.ProductId}, VariantId: {srcItem.VariantId?.ToString() ?? "null"})"
                                );
                            }

                            // بررسی موجودی کل
                            if (seg.Qty > srcItem.OnHand)
                            {
                                throw new InvalidOperationException(
                                    $"موجودی کل کافی نیست برای ارسال در StockItem {srcItem.Id}. " +
                                    $"SKU: {srcItem.Sku}, " +
                                    $"موجودی کل: {srcItem.OnHand}, " +
                                    $"مقدار درخواستی: {seg.Qty}, " +
                                    $"رزرو شده: {srcItem.Reserved}, " +
                                    $"مسدود شده: {srcItem.Blocked}. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id}, " +
                                    $"ProductId: {srcItem.ProductId}, VariantId: {srcItem.VariantId?.ToString() ?? "null"})"
                                );
                            }

                            try
                            {
                                // مرحله 1: آزاد کردن رزرو (Release) - چون قبلاً برای تخصیص رزرو شده بود
                                srcItem.Release(seg.Qty);
                            }
                            catch (InvalidOperationException ex)
                            {
                                throw new InvalidOperationException(
                                    $"خطا در آزاد کردن رزرو برای StockItem {srcItem.Id} (SKU: {srcItem.Sku}): {ex.Message}. " +
                                    $"رزرو شده: {srcItem.Reserved}, مقدار درخواستی: {seg.Qty}. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id})",
                                    ex
                                );
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                throw new ArgumentException(
                                    $"مقدار نامعتبر برای آزاد کردن رزرو: {seg.Qty}. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id}, StockItemId: {srcItem.Id})",
                                    ex
                                );
                            }

                            try
                            {
                                // مرحله 2: کم کردن از موجودی (Decrease) - حالا که رزرو آزاد شد، Available کافی است
                                srcItem.Decrease(seg.Qty);
                            }
                            catch (InvalidOperationException ex)
                            {
                                // Wrap with more context
                                throw new InvalidOperationException(
                                    $"خطا در کاهش موجودی برای StockItem {srcItem.Id} (SKU: {srcItem.Sku}): {ex.Message}. " +
                                    $"موجودی آزاد: {srcItem.Available}, مقدار درخواستی: {seg.Qty}. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id})",
                                    ex
                                );
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                throw new ArgumentException(
                                    $"مقدار نامعتبر برای کاهش موجودی: {seg.Qty}. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id}, StockItemId: {srcItem.Id})",
                                    ex
                                );
                            }

                            try
                            {
                                var led = StockLedgerEntry.Create(
                                    timestampUtc: DateTime.UtcNow,
                                    productId: srcItem.ProductId,
                                    variantId: srcItem.VariantId,
                                    warehouseId: srcItem.WarehouseId, // مبدا
                                    lotNumber: srcItem.LotNumber,
                                    expiryDate: srcItem.ExpiryDate,
                                    deltaQty: -seg.Qty,
                                    type: StockMovementType.TransferOut,
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
                                    $"خطا در ایجاد رکورد کاردکس برای StockItem {srcItem.Id} (SKU: {srcItem.Sku}): {ex.Message}. " +
                                    $"(TransferId: {tr.Id}, LineId: {line.Id}, SegmentId: {seg.Id})",
                                    ex
                                );
                            }

                            await TransferSerialsHelper.MarkInTransitAsync(_db, seg.Id, ct);
                        }
                    }

                    tr.Ship(req.WhenUtc);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();

                    // Reload transfer for retry
                    tr = await _db.Transfers
                        .Include(t => t.Lines)
                        .ThenInclude(l => l.Segments)
                        .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                        ?? throw new InvalidOperationException(
                            $"سند انتقال {req.TransferId} در تلاش مجدد {attempt} یافت نشد."
                        );

                    // Re-validate after reload
                    if (tr.Status != TransferStatus.Draft)
                        throw new InvalidOperationException(
                            $"وضعیت سند انتقال {req.TransferId} در تلاش مجدد {attempt} تغییر کرده است. " +
                            $"وضعیت فعلی: {tr.Status}"
                        );

                    if (tr.Lines.Count == 0)
                        throw new InvalidOperationException(
                            $"سند انتقال {req.TransferId} در تلاش مجدد {attempt} بدون خط است."
                        );

                    if (tr.Lines.Any(l => l.RemainingQty > 0))
                        throw new InvalidOperationException(
                            $"سند انتقال {req.TransferId} در تلاش مجدد {attempt} دارای خطوط تخصیص نشده است."
                        );
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
                        $"خطا در ارسال سند انتقال {req.TransferId} پس از {maxAttempts} تلاش: {ex.Message}",
                        ex
                    );
                }
            }
        });

        return Unit.Value;
    }
}
