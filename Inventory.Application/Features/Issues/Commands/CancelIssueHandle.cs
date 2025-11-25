using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class CancelIssueHandler : IRequestHandler<CancelIssueCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public CancelIssueHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(CancelIssueCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var issue = await _db.Issues
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
            issue.ClearAllocations(l.Id); 
        }

        issue.Cancel();
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}