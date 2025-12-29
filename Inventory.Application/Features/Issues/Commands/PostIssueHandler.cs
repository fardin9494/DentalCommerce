using Inventory.Domain.Aggregates;
using Inventory.Application.Features.Issues.Serials;
using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class PostIssueHandler : IRequestHandler<PostIssueCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public PostIssueHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<Unit> Handle(PostIssueCommand req, CancellationToken ct)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _tx.ExecuteAsync(async ct =>
                {
                    var issue = await _db.Issues
                        .AsSplitQuery()
                        .Include(i => i.Lines)
                        .ThenInclude(l => l.Allocations)
                        .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                        ?? throw new InvalidOperationException("سفارش برداشت پیدا نشد.");

                    var when = req.WhenUtc ?? DateTime.UtcNow;
                    
                    try
                    {
                        issue.Post(when);
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException($"خطا در ثبت خروجی: {ex.Message}", ex);
                    }

                    foreach (var line in issue.Lines)
                    {
                        if (line.Allocations.Count == 0)
                        {
                            throw new InvalidOperationException(
                                $"خط {line.LineNo} تخصیص داده نشده است. قبل از ثبت، باید تمام خطوط تخصیص داده شوند."
                            );
                        }

                        foreach (var alloc in line.Allocations)
                        {
                            var stock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == alloc.StockItemId, ct);
                            if (stock is null)
                            {
                                throw new InvalidOperationException(
                                    $"StockItem با شناسه {alloc.StockItemId} برای تخصیص خط {line.LineNo} پیدا نشد."
                                );
                            }

                            // فقط کالاهای Available قابل فروش هستند
                            if (stock.Status != StockStatus.Available && stock.Status != StockStatus.Reserved)
                            {
                                throw new InvalidOperationException(
                                    $"کالا با شناسه {stock.Id} (خط {line.LineNo}) در وضعیت {stock.Status} است و قابل فروش نیست. فقط کالاهای آزاد قابل فروش هستند."
                                );
                            }

                            // بررسی موجودی رزرو شده
                            if (stock.Reserved < alloc.Qty)
                            {
                                throw new InvalidOperationException(
                                    $"موجودی رزرو شده کافی نیست برای StockItem {stock.Id} (خط {line.LineNo}). " +
                                    $"رزرو شده: {stock.Reserved}, مورد نیاز: {alloc.Qty}"
                                );
                            }

                            // بررسی موجودی OnHand
                            await IssueSerialsHelper.MarkIssuedAsync(_db, stock.Id, issue.Id, line.Id, alloc.Qty, when, ct);

                            if (stock.OnHand < alloc.Qty)
                            {
                                throw new InvalidOperationException(
                                    $"موجودی OnHand کافی نیست برای StockItem {stock.Id} (خط {line.LineNo}). " +
                                    $"OnHand: {stock.OnHand}, مورد نیاز: {alloc.Qty}"
                                );
                            }

                            try
                            {
                                stock.Release(alloc.Qty);
                                stock.Decrease(alloc.Qty);
                            }
                            catch (InvalidOperationException ex)
                            {
                                throw new InvalidOperationException(
                                    $"خطا در کاهش موجودی برای StockItem {stock.Id} (خط {line.LineNo}): {ex.Message}",
                                    ex
                                );
                            }

                            var costRecord = await _db.InventoryCosts
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.StockItemId == stock.Id, ct);

                            try
                            {
                                var entry = StockLedgerEntry.Create(
                                    timestampUtc: when,
                                    productId: line.ProductId,
                                    variantId: line.VariantId,
                                    warehouseId: issue.WarehouseId ?? stock.WarehouseId, // استفاده از warehouseId موجود در StockItem اگر Issue.WarehouseId null باشد
                                    lotNumber: stock.LotNumber,
                                    expiryDate: stock.ExpiryDate,
                                    deltaQty: -alloc.Qty,
                                    type: StockMovementType.Issue,
                                    refDocType: nameof(Issue),
                                    refDocId: issue.Id,
                                    unitCost: costRecord?.Amount,
                                    note: $"Issued to Order {issue.ExternalRef ?? "بدون مرجع"} - Shelf: {stock.ShelfId?.ToString() ?? "بدون قفسه"}"
                                );
                                _db.StockLedger.Add(entry);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"خطا در ایجاد StockLedgerEntry برای خط {line.LineNo}: {ex.Message}",
                                    ex
                                );
                            }
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                }, ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
        }

        return Unit.Value;
    }
}
