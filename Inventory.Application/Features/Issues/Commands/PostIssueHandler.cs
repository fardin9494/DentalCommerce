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
        var issue = await _db.Issues
            .Include(i => i.Lines)
            .ThenInclude(l => l.Allocations)
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct);

        if (issue is null)
            throw new InvalidOperationException("سند خروج یافت نشد.");

        if (issue.Status != IssueStatus.Draft)
            throw new InvalidOperationException("فقط سند پیش‌نویس قابل پست است.");

        if (issue.Lines.Count == 0)
            throw new InvalidOperationException("سند خروج بدون آیتم قابل پست نیست.");

        if (issue.Lines.Any(l => l.RemainingQty > 0))
            throw new InvalidOperationException("قبل از پست، تمام خطوط باید کامل تخصیص داده شوند.");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    // هر تخصیص: از StockItem مربوطه کم کن و دفتر را ثبت کن
                    foreach (var line in issue.Lines)
                    {
                        foreach (var a in line.Allocations)
                        {
                            var si = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == a.StockItemId, ct)
                                ?? throw new InvalidOperationException("StockItem تخصیص‌یافته یافت نشد.");

                            // کم کردن از موجودی
                            si.Decrease(a.Qty);

                            var ledger = StockLedgerEntry.Create(
                                timestampUtc: DateTime.UtcNow,
                                productId: si.ProductId,
                                variantId: si.VariantId,
                                warehouseId: si.WarehouseId,
                                lotNumber: si.LotNumber,
                                expiryDate: si.ExpiryDate,
                                deltaQty: -a.Qty,
                                type: StockMovementType.Issue,
                                refDocType: nameof(Issue),
                                refDocId: issue.Id,
                                unitCost: null,
                                note: issue.ExternalRef
                            );
                            _db.StockLedger.Add(ledger);
                        }
                    }

                    issue.Post(req.WhenUtc);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();

                    issue = await _db.Issues
                        .Include(i => i.Lines)
                        .ThenInclude(l => l.Allocations)
                        .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                        ?? throw new InvalidOperationException("سند خروج در تلاش مجدد یافت نشد.");

                    if (attempt == maxAttempts)
                        throw;
                }
            }
        });

        return Unit.Value;
    }
}
