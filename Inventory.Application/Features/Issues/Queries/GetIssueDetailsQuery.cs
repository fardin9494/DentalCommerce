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
    decimal Qty,
    string? Sku,
    string? LotNumber,
    DateTime? ExpiryDate,
    Guid? ShelfId,
    string? ShelfName
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

        // Get all StockItem IDs from allocations
        var stockItemIds = issue.Lines
            .SelectMany(l => l.Allocations)
            .Select(a => a.StockItemId)
            .Distinct()
            .ToList();

        // Load stock items with their details
        var stockItems = await _db.StockItems
            .AsNoTracking()
            .Where(si => stockItemIds.Contains(si.Id))
            .Select(si => new
            {
                si.Id,
                si.Sku,
                si.LotNumber,
                si.ExpiryDate,
                si.ShelfId
            })
            .ToListAsync(ct);

        // Get shelf names
        var shelfIds = stockItems.Where(si => si.ShelfId.HasValue).Select(si => si.ShelfId!.Value).Distinct().ToList();
        var shelves = shelfIds.Count > 0
            ? await _db.StockShelves
                .AsNoTracking()
                .Where(s => shelfIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        var stockItemsDict = stockItems.ToDictionary(si => si.Id, si => new
        {
            si.Sku,
            si.LotNumber,
            si.ExpiryDate,
            si.ShelfId,
            ShelfName = si.ShelfId.HasValue && shelves.TryGetValue(si.ShelfId.Value, out var name) ? name : null
        });

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
                    .Select(a =>
                    {
                        var stockInfo = stockItemsDict.TryGetValue(a.StockItemId, out var info) ? info : null;
                        return new IssueAllocationDto(
                            a.Id,
                            a.StockItemId,
                            a.Qty,
                            stockInfo?.Sku,
                            stockInfo?.LotNumber,
                            stockInfo?.ExpiryDate,
                            stockInfo?.ShelfId,
                            stockInfo?.ShelfName
                        );
                    })
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


