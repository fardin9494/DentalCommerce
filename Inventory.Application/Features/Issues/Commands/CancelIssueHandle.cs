using Inventory.Application.Abstractions;
using Inventory.Application.Features.Issues.Serials;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class CancelIssueHandler : IRequestHandler<CancelIssueCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public CancelIssueHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<Unit> Handle(CancelIssueCommand req, CancellationToken ct)
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
                                    .Include(i => i.Lines).ThenInclude(l => l.Allocations)
                                    .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                                ?? throw new InvalidOperationException("سند خروج پیدا نشد.");

                    // آزاد کردن رزروهای فعلی
                    foreach (var l in issue.Lines.ToList())
                    {
                        foreach (var a in l.Allocations.ToList())
                        {
                            var si = await _db.StockItems.FirstAsync(x => x.Id == a.StockItemId, ct);
                            si.Release(a.Qty);
                        }
                        await IssueSerialsHelper.ReleaseReservedSerialsAsync(_db, l.Id, ct);
                        issue.ClearAllocations(l.Id);
                    }

                    issue.Cancel();
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
