using Inventory.Application.Abstractions;
using Inventory.Application.Features.Issues.Serials;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class UpdateIssueLineHandler : IRequestHandler<UpdateIssueLineCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public UpdateIssueLineHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateIssueLineCommand req, CancellationToken ct)
    {
        var issue = await _db.Issues
            .Include(i => i.Lines)
            .ThenInclude(l => l.Allocations)
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
            ?? throw new InvalidOperationException("سند خروج پیدا نشد.");
        
        var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("خط سند خروج پیدا نشد.");

        // اگر مقدار جدید کمتر از مقدار تخصیص داده شده است، باید تخصیص‌ها را آزاد کنیم
        if (req.Qty < line.AllocatedQty)
        {
            // آزاد کردن تمام تخصیص‌ها
            foreach (var alloc in line.Allocations)
            {
                var stock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == alloc.StockItemId, ct);
                if (stock != null)
                {
                    stock.Release(alloc.Qty);
                }
            }
            await IssueSerialsHelper.ReleaseReservedSerialsAsync(_db, line.Id, ct);
            issue.ClearAllocations(req.LineId);
        }

        line.UpdateQty(req.Qty);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

