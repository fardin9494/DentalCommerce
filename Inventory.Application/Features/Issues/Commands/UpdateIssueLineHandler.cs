using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class UpdateIssueLineHandler : IRequestHandler<UpdateIssueLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateIssueLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateIssueLineCommand req, CancellationToken ct)
    {
        var issue = await _db.Issues.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                    ?? throw new InvalidOperationException("سند خروج پیدا نشد.");
        var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("خط سند خروج پیدا نشد.");
        line.UpdateQty(req.Qty);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

