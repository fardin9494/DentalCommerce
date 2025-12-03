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
                    issue.Post(when);

                    foreach (var line in issue.Lines)
                    {
                        foreach (var alloc in line.Allocations)
                        {
                            var stock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == alloc.StockItemId, ct)
                                ?? throw new InvalidOperationException("موجودی انتخاب‌شده پیدا نشد.");

                            stock.Release(alloc.Qty);
                            stock.Decrease(alloc.Qty);

                            var costRecord = await _db.InventoryCosts
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.StockItemId == stock.Id, ct);

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
                                note: $"Issued to Order {issue.ExternalRef} - Shelf: {stock.ShelfId}"
                            );
                            _db.StockLedger.Add(entry);
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