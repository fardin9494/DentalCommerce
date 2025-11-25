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
            .Include(i => i.Lines).ThenInclude(l => l.Allocations)
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
            ?? throw new InvalidOperationException("سند خروج پیدا نشد.");

        if (issue.Lines.Count == 0)
            throw new InvalidOperationException("سند خروج بدون آیتم قابل پست نیست.");
        if (issue.Lines.Any(l => l.RemainingQty > 0))
            throw new InvalidOperationException("ابتدا همه‌ی خطوط را کامل تخصیص دهید.");

        var when = DateTime.SpecifyKind(req.WhenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        foreach (var l in issue.Lines)
        {
            foreach (var a in l.Allocations)
            {
                var si = await _db.StockItems.FirstAsync(x => x.Id == a.StockItemId, ct);

                // اول آزادسازی رزرو، بعد کاهش موجودی
                si.Release(a.Qty);
                si.Decrease(a.Qty);

                var ledger = StockLedgerEntry.Create(
                    timestampUtc: when,
                    productId: l.ProductId,
                    variantId: l.VariantId,
                    warehouseId: issue.WarehouseId,
                    lotNumber: si.LotNumber,
                    expiryDate: si.ExpiryDate,
                    deltaQty: -a.Qty,
                    type: StockMovementType.Issue,
                    refDocType: "Issue",
                    refDocId: issue.Id,
                    unitCost: null,
                    note: null
                );
                _db.StockLedger.Add(ledger);
            }
        }

        issue.Post(when);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}
