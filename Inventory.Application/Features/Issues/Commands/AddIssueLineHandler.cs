using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class AddIssueLineHandler : IRequestHandler<AddIssueLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public AddIssueLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(AddIssueLineCommand req, CancellationToken ct)
    {
        var issue = await _db.Issues.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                    ?? throw new InvalidOperationException("سند خروج پیدا نشد.");

        var line = issue.AddLine(req.ProductId, req.VariantId, req.Qty);
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}