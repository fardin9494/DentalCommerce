using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class ApproveReceiptLinePartialHandler : IRequestHandler<ApproveReceiptLinePartialCommand, Unit>
{
    private readonly InventoryDbContext _db;

    public ApproveReceiptLinePartialHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ApproveReceiptLinePartialCommand req, CancellationToken ct)
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
                        throw new InvalidOperationException("فقط رسیدهای دریافت شده قابل تایید جزئی هستند.");

                    var line = rec.FindLine(req.LineId)
                        ?? throw new InvalidOperationException("خط رسید پیدا نشد.");

                    // تایید جزئی خط (مقدار جدید)
                    line.ApprovePartial(req.Qty);

                    // پیدا کردن StockItem مربوطه
                    var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                        si.ProductId == line.ProductId &&
                        si.VariantId == line.VariantId &&
                        si.WarehouseId == rec.WarehouseId &&
                        si.LotNumber == line.LotNumber &&
                        si.ExpiryDate == line.ExpiryDate, ct)
                        ?? throw new InvalidOperationException($"موجودی مربوط به خط {line.LineNo} یافت نشد.");

                    // منطق ساده: Blocked = ApprovedQty + RemainingQty
                    // RemainingQty = Qty - ApprovedQty - RejectedQty
                    var remainingQty = line.Qty - line.ApprovedQty - line.RejectedQty;
                    var newBlocked = line.ApprovedQty + remainingQty;
                    
                    // اگر در قفسه نیست، Blocked را تنظیم می‌کنیم
                    if (!stock.ShelfId.HasValue)
                    {
                        // اگر ApprovedQty > 0 است، reason "Awaiting Shelving" است
                        // در غیر این صورت reason "Quarantine - Waiting for Approval" است
                        var blockReason = line.ApprovedQty > 0 
                            ? "Awaiting Shelving" 
                            : "Quarantine - Waiting for Approval";
                        
                        stock.SetBlocked(newBlocked, blockReason);
                    }
                    else
                    {
                        // اگر در قفسه است، فقط مقدار Blocked را کاهش می‌دهیم (از "Quarantine" Unblock)
                        // ApprovedQty از Blocked خارج می‌شود و Available می‌شود
                        var currentBlocked = stock.Blocked;
                        if (newBlocked < currentBlocked)
                        {
                            stock.Unblock(currentBlocked - newBlocked);
                        }
                        else if (newBlocked > currentBlocked)
                        {
                            // این حالت نباید اتفاق بیفتد اگر در قفسه است
                            throw new InvalidOperationException("نمی‌توان مقدار مسدود را در قفسه افزایش داد.");
                        }
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
