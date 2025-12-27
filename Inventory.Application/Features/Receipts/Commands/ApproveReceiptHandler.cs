using Inventory.Application.Features.Receipts.Serials;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class ApproveReceiptCommand : IRequest<Unit>
{
    public Guid ReceiptId { get; set; }
}

public sealed class ApproveReceiptHandler : IRequestHandler<ApproveReceiptCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public ApproveReceiptHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ApproveReceiptCommand req, CancellationToken ct)
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
                    var rec = await _db.Receipts.Include(r => r.Lines)
                        .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                        ?? throw new InvalidOperationException("رسید پیدا نشد.");

                    // تایید نهایی - فقط وقتی همه خطوط تکلیفشان مشخص شده باشد
                    rec.FinalApprove();

                    // آزاد کردن فقط مقادیر تایید شده از قرنطینه
                    // توجه: ممکن است بخشی از مقادیر قبلاً در ApproveReceiptLinePartialHandler آزاد شده باشند
                    foreach (var l in rec.Lines)
                    {
                        if (l.ApprovedQty > 0)
                        {
                            var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                                si.ProductId == l.ProductId &&
                                si.VariantId == l.VariantId &&
                                si.WarehouseId == rec.WarehouseId &&
                                si.LotNumber == l.LotNumber &&
                                si.ExpiryDate == l.ExpiryDate, ct)
                                ?? throw new InvalidOperationException($"موجودی مربوط به خط {l.LineNo} یافت نشد.");

                            await ReceiptSerialsHelper.SyncLineSerialStatusesAsync(_db, l, stock.ShelfId.HasValue, ct);

                            // آزاد کردن فقط مقدار باقیمانده Blocked (ممکن است بخشی قبلاً در ApproveReceiptLinePartialHandler آزاد شده باشد)
                            var remainingBlocked = stock.Blocked;
                            if (remainingBlocked > 0)
                            {
                                // آزاد کردن از Blocked
                                stock.Unblock(remainingBlocked);
                                
                                // اگر در قفسه است → Available می‌شود
                                // اگر در قفسه نیست → AwaitingShelving می‌شود (تا زمانی که در قفسه چیده شود)
                                if (stock.ShelfId.HasValue)
                                {
                                    // در قفسه است → Available (Unblock کافی است)
                                }
                                else
                                {
                                    // در قفسه نیست → AwaitingShelving می‌شود
                                    stock.Block(remainingBlocked, "Awaiting Shelving");
                                }
                            }
                            // اگر remainingBlocked == 0، یعنی همه مقادیر قبلاً آزاد شده‌اند و نیازی به Unblock نیست
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    // Log the exception for debugging
                    throw new InvalidOperationException(
                        $"خطا در تایید نهایی رسید (تلاش {attempt}/{maxAttempts}): {ex.Message}. " +
                        $"Inner: {ex.InnerException?.Message}", ex);
                }
            }
        });

        return Unit.Value;
    }
}

