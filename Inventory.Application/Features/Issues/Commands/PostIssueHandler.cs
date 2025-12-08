using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class PostIssueHandler : IRequestHandler<PostIssueCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public PostIssueHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(PostIssueCommand req, CancellationToken ct)
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
                    var issue = await _db.Issues
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

                            // بررسی موجودی رزرو شده
                            if (stock.Reserved < alloc.Qty)
                            {
                                throw new InvalidOperationException(
                                    $"موجودی رزرو شده کافی نیست برای StockItem {stock.Id} (خط {line.LineNo}). " +
                                    $"رزرو شده: {stock.Reserved}, مورد نیاز: {alloc.Qty}"
                                );
                            }

                            // بررسی موجودی OnHand
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
                                    warehouseId: issue.WarehouseId,
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