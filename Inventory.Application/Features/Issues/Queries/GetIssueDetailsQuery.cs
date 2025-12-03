using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Queries;

public sealed record IssueDetailsQuery(Guid Id) : IRequest<IssueDetailsDto?>;

public sealed record IssueDetailsDto(
    Guid Id,
    Guid WarehouseId,
    IssueStatus Status,
    string? ExternalRef,
    DateTime DocDate,
    DateTime? PostedAt,
    IReadOnlyList<IssueLineDto> Lines
);

public sealed record IssueLineDto(
    Guid Id,
    int LineNo,
    Guid ProductId,
    Guid? VariantId,
    decimal RequestedQty,
    decimal AllocatedQty,
    decimal RemainingQty,
    IReadOnlyList<IssueAllocationDto> Allocations
);

public sealed record IssueAllocationDto(
    Guid Id,
    Guid StockItemId,
    decimal Qty
);

public sealed class GetIssueDetailsHandler : IRequestHandler<IssueDetailsQuery, IssueDetailsDto?>
{
    private readonly InventoryDbContext _db;
    public GetIssueDetailsHandler(InventoryDbContext db) => _db = db;

    public async Task<IssueDetailsDto?> Handle(IssueDetailsQuery req, CancellationToken ct)
    {
        var issue = await _db.Issues
            .AsNoTracking()
            .Include(i => i.Lines)
            .ThenInclude(l => l.Allocations)
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (issue is null) return null;

        var lines = issue.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new IssueLineDto(
                l.Id,
                l.LineNo,
                l.ProductId,
                l.VariantId,
                l.RequestedQty,
                l.AllocatedQty,
                l.RemainingQty,
                l.Allocations
                    .Select(a => new IssueAllocationDto(a.Id, a.StockItemId, a.Qty))
                    .ToList()
            ))
            .ToList();

        return new IssueDetailsDto(
            issue.Id,
            issue.WarehouseId,
            issue.Status,
            issue.ExternalRef,
            issue.DocDate,
            issue.PostedAt,
            lines
        );
    }
}


