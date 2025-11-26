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
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var issue = await _db.Issues
            .Include(i => i.Lines)
            .ThenInclude(l => l.Allocations)
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct);

        if (issue is null) throw new InvalidOperationException("سند خروج یافت نشد.");

        var when = req.WhenUtc ?? DateTime.UtcNow;

        // تغییر وضعیت سند به Posted
        issue.Post(when);

        // پردازش تمام خطوط و تخصیص‌ها
        foreach (var line in issue.Lines)
        {
            foreach (var alloc in line.Allocations)
            {
                var stock = await _db.StockItems.FindAsync(new object[] { alloc.StockItemId }, ct);
                if (stock is null) throw new InvalidOperationException("رکورد موجودی یافت نشد.");

                // 1. کسر نهایی از انبار (تبدیل رزرو به خروج قطعی)
                stock.Release(alloc.Qty); // حذف رزرو
                stock.Decrease(alloc.Qty); // کم کردن OnHand

                // 2. پیدا کردن قیمت تمام شده (Cost) برای ثبت در سود و زیان
                var costRecord = await _db.InventoryCosts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.StockItemId == stock.Id, ct);

                // 3. ثبت در کاردکس (Ledger)
                var entry = StockLedgerEntry.Create(
                    timestampUtc: when,
                    productId: line.ProductId,
                    variantId: line.VariantId,
                    warehouseId: issue.WarehouseId,
                    lotNumber: stock.LotNumber,
                    expiryDate: stock.ExpiryDate,
                    deltaQty: -alloc.Qty, // خروج منفی است
                    type: StockMovementType.Issue,
                    refDocType: nameof(Issue),
                    refDocId: issue.Id,
                    unitCost: costRecord?.Amount, // <--- قیمت خرید اینجا ثبت می‌شود
                    note: $"Issued to Order {issue.ExternalRef} - Shelf: {stock.ShelfId}"
                );
                _db.StockLedger.Add(entry);
            }
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}