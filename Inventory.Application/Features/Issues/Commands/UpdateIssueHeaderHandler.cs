using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class UpdateIssueHeaderHandler : IRequestHandler<UpdateIssueHeaderCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public UpdateIssueHeaderHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateIssueHeaderCommand req, CancellationToken ct)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                    ?? throw new InvalidOperationException("سند خروج پیدا نشد.");
        issue.UpdateHeader(req.ExternalRef, req.DocDateUtc);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

