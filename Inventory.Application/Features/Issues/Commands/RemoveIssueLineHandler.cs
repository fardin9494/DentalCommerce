using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class RemoveIssueLineHandler : IRequestHandler<RemoveIssueLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public RemoveIssueLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(RemoveIssueLineCommand req, CancellationToken ct)
    {
        var issue = await _db.Issues
            .Include(i => i.Lines)
            .ThenInclude(l => l.Allocations)
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
            ?? throw new InvalidOperationException("سند خروج پیدا نشد.");

        var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId)
            ?? throw new InvalidOperationException("خط سند خروج پیدا نشد.");

        // آزاد کردن رزروهای مربوط به این خط
        foreach (var alloc in line.Allocations)
        {
            var stock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == alloc.StockItemId, ct);
            if (stock != null)
            {
                stock.Release(alloc.Qty);
            }
        }

        issue.RemoveLine(req.LineId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

