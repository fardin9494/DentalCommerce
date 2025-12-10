using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class RejectReceiptLineHandler : IRequestHandler<RejectReceiptLineCommand, Unit>
{
    private readonly InventoryDbContext _db;

    public RejectReceiptLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(RejectReceiptLineCommand req, CancellationToken ct)
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
                    var rec = await _db.Receipts
                        .Include(r => r.Lines)
                        .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                        ?? throw new InvalidOperationException("رسید پیدا نشد.");

                    if (rec.Status != ReceiptStatus.Received)
                        throw new InvalidOperationException("فقط رسیدهای دریافت شده قابل رد هستند.");

                    var line = rec.FindLine(req.LineId)
                        ?? throw new InvalidOperationException("خط رسید پیدا نشد.");

                    // محاسبه مقدار تغییر برای ثبت در Ledger
                    var oldRejectedQty = line.RejectedQty;
                    var deltaQty = req.Qty - oldRejectedQty;

                    // رد کردن خط (مقدار جدید)
                    line.Reject(req.Qty, req.Reason);

                    // پیدا کردن StockItem مربوطه
                    var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                        si.ProductId == line.ProductId &&
                        si.VariantId == line.VariantId &&
                        si.WarehouseId == rec.WarehouseId &&
                        si.LotNumber == line.LotNumber &&
                        si.ExpiryDate == line.ExpiryDate, ct)
                        ?? throw new InvalidOperationException($"موجودی مربوط به خط {line.LineNo} یافت نشد.");

                    // منطق ساده: OnHand = Qty - RejectedQty
                    // Blocked = ApprovedQty + RemainingQty
                    // RemainingQty = Qty - ApprovedQty - RejectedQty
                    // پس: Blocked = ApprovedQty + (Qty - ApprovedQty - RejectedQty) = Qty - RejectedQty = OnHand
                    var newOnHand = line.Qty - line.RejectedQty;
                    var remainingQty = line.Qty - line.ApprovedQty - line.RejectedQty;
                    var newBlocked = line.ApprovedQty + remainingQty; // این برابر با newOnHand است
                    
                    // محاسبه تغییرات
                    var onHandDelta = newOnHand - stock.OnHand;
                    
                    // اگر در قفسه نیست
                    if (!stock.ShelfId.HasValue)
                    {
                        // ابتدا OnHand را تنظیم می‌کنیم
                        if (onHandDelta < 0)
                        {
                            // کاهش OnHand (از Blocked کم می‌کنیم)
                            stock.DecreaseFromBlocked(-onHandDelta);
                        }
                        else if (onHandDelta > 0)
                        {
                            // افزایش OnHand (بازگشت از رد شده)
                            stock.Increase(onHandDelta);
                        }
                        
                        // سپس Blocked را تنظیم می‌کنیم
                        // چون newBlocked = newOnHand و OnHand قبلاً تنظیم شده، پس newBlocked باید برابر با OnHand فعلی باشد
                        // اما برای اطمینان، از stock.OnHand استفاده می‌کنیم (که بعد از Increase/Decrease به‌روز شده)
                        var blockReason = line.ApprovedQty > 0 
                            ? "Awaiting Shelving" 
                            : "Quarantine - Waiting for Approval";
                        // استفاده از stock.OnHand به جای newBlocked برای اطمینان از اینکه Blocked = OnHand
                        stock.SetBlocked(stock.OnHand, blockReason);
                    }
                    else
                    {
                        // اگر در قفسه است
                        if (onHandDelta < 0)
                        {
                            // کاهش OnHand (از Blocked کم می‌کنیم)
                            stock.DecreaseFromBlocked(-onHandDelta);
                        }
                        else if (onHandDelta > 0)
                        {
                            // افزایش OnHand (بازگشت از رد شده)
                            stock.Increase(onHandDelta);
                        }
                        
                        // اگر در قفسه است، Blocked را کاهش می‌دهیم (چون ApprovedQty از Blocked خارج می‌شود)
                        var blockedDelta = newBlocked - stock.Blocked;
                        if (blockedDelta < 0)
                        {
                            stock.Unblock(-blockedDelta);
                        }
                    }
                    
                    // ثبت در StockLedger
                    if (deltaQty != 0)
                    {
                        StockLedgerEntry ledgerEntry;
                        if (deltaQty > 0)
                        {
                            // افزایش رد شده: کاهش موجودی (AdjustmentMinus با deltaQty منفی)
                            ledgerEntry = StockLedgerEntry.Create(
                                timestampUtc: DateTime.UtcNow,
                                productId: line.ProductId,
                                variantId: line.VariantId,
                                warehouseId: rec.WarehouseId,
                                lotNumber: line.LotNumber,
                                expiryDate: line.ExpiryDate,
                                deltaQty: -deltaQty, // منفی برای کاهش موجودی
                                type: StockMovementType.AdjustmentMinus,
                                refDocType: nameof(Receipt),
                                refDocId: rec.Id,
                                unitCost: line.UnitCost,
                                note: $"Rejected from Receipt - {req.Reason ?? "بدون دلیل"}"
                            );
                        }
                        else
                        {
                            // کاهش رد شده: افزایش موجودی (AdjustmentPlus با deltaQty مثبت)
                            ledgerEntry = StockLedgerEntry.Create(
                                timestampUtc: DateTime.UtcNow,
                                productId: line.ProductId,
                                variantId: line.VariantId,
                                warehouseId: rec.WarehouseId,
                                lotNumber: line.LotNumber,
                                expiryDate: line.ExpiryDate,
                                deltaQty: -deltaQty, // مثبت (چون deltaQty منفی است)
                                type: StockMovementType.AdjustmentPlus,
                                refDocType: nameof(Receipt),
                                refDocId: rec.Id,
                                unitCost: line.UnitCost,
                                note: $"Re-rejected from Receipt - Returned to Quarantine"
                            );
                        }
                        _db.StockLedger.Add(ledgerEntry);
                    }

                    // بررسی تکلیف خطوط (بدون تایید خودکار)

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
            }
        });

        return Unit.Value;
    }
}

