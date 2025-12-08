using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Queries;

public sealed record GetIssuesListQuery(
    Guid? WarehouseId = null,
    IssueStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<IssuesListResult>;

public sealed record IssuesListResult(
    IReadOnlyList<IssueListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record IssueListItemDto(
    Guid Id,
    Guid WarehouseId,
    string? WarehouseName,
    string Status,
    string? ExternalRef,
    DateTime DocDate,
    DateTime? PostedAt,
    int LinesCount,
    decimal TotalRequestedQty,
    decimal TotalAllocatedQty,
    decimal TotalRemainingQty,
    DateTime CreatedAt
);

public sealed class GetIssuesListHandler : IRequestHandler<GetIssuesListQuery, IssuesListResult>
{
    private readonly InventoryDbContext _db;
    public GetIssuesListHandler(InventoryDbContext db) => _db = db;

    public async Task<IssuesListResult> Handle(GetIssuesListQuery req, CancellationToken ct)
    {
        var query = _db.Issues
            .AsNoTracking()
            .Include(i => i.Lines)
            .ThenInclude(l => l.Allocations)
            .AsQueryable();

        // Apply filters
        if (req.WarehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == req.WarehouseId.Value);

        if (req.Status.HasValue)
            query = query.Where(i => i.Status == req.Status.Value);

        if (req.FromDate.HasValue)
            query = query.Where(i => i.DocDate >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(i => i.DocDate <= req.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchTerm = req.Search.Trim().ToLower();
            query = query.Where(i => 
                (i.ExternalRef != null && i.ExternalRef.ToLower().Contains(searchTerm)) ||
                i.Id.ToString().ToLower().Contains(searchTerm)
            );
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Calculate pagination
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        // Get warehouses for names
        var warehouseIds = await query.Select(i => i.WarehouseId).Distinct().ToListAsync(ct);
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        // Get paginated results - fetch raw data first
        var rawItems = await query
            .OrderByDescending(i => i.DocDate)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Convert to DTOs with calculated values
        var items = rawItems.Select(i => new IssueListItemDto(
            i.Id,
            i.WarehouseId,
            warehouses.TryGetValue(i.WarehouseId, out var name) ? name : null,
            Enum.IsDefined(typeof(IssueStatus), i.Status) ? i.Status.ToString() : "Draft",
            i.ExternalRef,
            i.DocDate,
            i.PostedAt,
            i.Lines.Count,
            i.Lines.Sum(l => l.RequestedQty),
            i.Lines.Sum(l => l.AllocatedQty),
            i.Lines.Sum(l => l.RemainingQty),
            i.CreatedAt
        )).ToList();

        return new IssuesListResult(
            items,
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}

