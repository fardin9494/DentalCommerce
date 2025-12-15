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
            throw new InvalidOperationException("پیش‌نویس اصلاح موجودی پیدا نشد.");

        if (adj.Status != AdjustmentStatus.Draft)
            throw new InvalidOperationException("فقط پیش‌نویس‌ها قابل ثبت هستند.");

        if (adj.Lines.Count == 0)
            throw new InvalidOperationException("هیچ خطی برای ثبت وجود ندارد.");

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
                        try
                        {
                            var si = await _db.StockItems
                                .FirstOrDefaultAsync(x => x.Id == l.StockItemId, ct)
                                ?? throw new InvalidOperationException($"StockItem {l.StockItemId} برای خط {l.LineNo} پیدا نشد.");

                            if (si.WarehouseId != adj.WarehouseId)
                                throw new InvalidOperationException($"موجودی خط {l.LineNo} در انبار دیگری قرار دارد.");

                            if (si.ProductId != l.ProductId || si.VariantId != l.VariantId)
                                throw new InvalidOperationException($"موجودی خط {l.LineNo} با محصول/تنوع ثبت شده همخوانی ندارد.");

                            if (!string.Equals(si.LotNumber, l.LotNumber, StringComparison.OrdinalIgnoreCase) ||
                                si.ExpiryDate != l.ExpiryDate)
                            {
                                throw new InvalidOperationException($"لات/تاریخ انقضا موجودی خط {l.LineNo} با انتخاب اولیه متفاوت است.");
                            }

                            si.ForceAdjust(l.QtyDelta);

                            var mtype = l.QtyDelta > 0
                                ? StockMovementType.AdjustmentPlus
                                : StockMovementType.AdjustmentMinus;

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
                        catch (InvalidOperationException)
                        {
                            throw;
                        }
                        catch (Exception ex) when (ex is not InvalidOperationException)
                        {
                            throw new InvalidOperationException(
                                $"خطا در پردازش خط {l.LineNo}: {ex.Message}",
                                ex
                            );
                        }
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
                        ?? throw new InvalidOperationException("پیش‌نویس اصلاح موجودی بعد از تلاش مجدد یافت نشد.");

                    if (attempt == maxAttempts)
                        throw;
                }
                catch (InvalidOperationException)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    throw;
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    // retry
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    throw;
                }
            }
        });

        return Unit.Value;
    }
}
