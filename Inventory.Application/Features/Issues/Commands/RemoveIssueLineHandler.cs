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
        var issue = await _db.Issues.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                    ?? throw new InvalidOperationException("سند خروج پیدا نشد.");
        issue.RemoveLine(req.LineId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

