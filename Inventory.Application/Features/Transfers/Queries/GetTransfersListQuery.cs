using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Queries;

public sealed record GetTransfersListQuery(
    Guid? SourceWarehouseId = null,
    Guid? DestinationWarehouseId = null,
    TransferStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<TransfersListResult>;

public sealed record TransfersListResult(
    IReadOnlyList<TransferListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record TransferListItemDto(
    Guid Id,
    Guid SourceWarehouseId,
    string? SourceWarehouseName,
    Guid DestinationWarehouseId,
    string? DestinationWarehouseName,
    string Status,
    string? ExternalRef,
    DateTime DocDate,
    DateTime? ShippedAt,
    DateTime? CompletedAt,
    int LinesCount,
    decimal TotalQty,
    decimal TotalAllocatedQty,
    decimal TotalRemainingQty,
    DateTime CreatedAt
);

public sealed class GetTransfersListHandler : IRequestHandler<GetTransfersListQuery, TransfersListResult>
{
    private readonly IInventoryDbContext _db;
    public GetTransfersListHandler(IInventoryDbContext db) => _db = db;

    public async Task<TransfersListResult> Handle(GetTransfersListQuery req, CancellationToken ct)
    {
        var query = _db.Transfers
            .AsNoTracking()
            .Include(t => t.Lines)
            .AsQueryable();

        // Apply filters
        if (req.SourceWarehouseId.HasValue)
            query = query.Where(t => t.SourceWarehouseId == req.SourceWarehouseId.Value);

        if (req.DestinationWarehouseId.HasValue)
            query = query.Where(t => t.DestinationWarehouseId == req.DestinationWarehouseId.Value);

        if (req.Status.HasValue)
            query = query.Where(t => t.Status == req.Status.Value);

        if (req.FromDate.HasValue)
            query = query.Where(t => t.DocDate >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(t => t.DocDate <= req.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchTerm = req.Search.Trim().ToLower();
            query = query.Where(t =>
                (t.ExternalRef != null && t.ExternalRef.ToLower().Contains(searchTerm)) ||
                t.Id.ToString().ToLower().Contains(searchTerm)
            );
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Calculate pagination
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        // Get warehouses for names - need to get IDs before pagination
        var sourceWarehouseIds = await query.Select(t => t.SourceWarehouseId).Distinct().ToListAsync(ct);
        var destWarehouseIds = await query.Select(t => t.DestinationWarehouseId).Distinct().ToListAsync(ct);
        var warehouseIds = sourceWarehouseIds.Union(destWarehouseIds).Distinct().ToList();
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        // Get paginated results - need to include Segments to calculate AllocatedQty
        // Rebuild query with proper includes
        var queryWithIncludes = _db.Transfers
            .AsNoTracking()
            .Include(t => t.Lines)
            .ThenInclude(l => l.Segments)
            .AsQueryable();

        // Apply same filters
        if (req.SourceWarehouseId.HasValue)
            queryWithIncludes = queryWithIncludes.Where(t => t.SourceWarehouseId == req.SourceWarehouseId.Value);

        if (req.DestinationWarehouseId.HasValue)
            queryWithIncludes = queryWithIncludes.Where(t => t.DestinationWarehouseId == req.DestinationWarehouseId.Value);

        if (req.Status.HasValue)
            queryWithIncludes = queryWithIncludes.Where(t => t.Status == req.Status.Value);

        if (req.FromDate.HasValue)
            queryWithIncludes = queryWithIncludes.Where(t => t.DocDate >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            queryWithIncludes = queryWithIncludes.Where(t => t.DocDate <= req.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchTerm = req.Search.Trim().ToLower();
            queryWithIncludes = queryWithIncludes.Where(t =>
                (t.ExternalRef != null && t.ExternalRef.ToLower().Contains(searchTerm)) ||
                t.Id.ToString().ToLower().Contains(searchTerm)
            );
        }

        var rawItems = await queryWithIncludes
            .OrderByDescending(t => t.DocDate)
            .ThenByDescending(t => t.Id) // Use Id for secondary sort instead of CreatedAt
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var itemsData = rawItems.Select(t => new
        {
            t.Id,
            t.SourceWarehouseId,
            t.DestinationWarehouseId,
            t.Status,
            t.ExternalRef,
            t.DocDate,
            t.ShippedAt,
            t.CompletedAt,
            LinesCount = t.Lines.Count,
            TotalQty = t.Lines.Sum(l => l.RequestedQty),
            TotalAllocatedQty = t.Lines.Sum(l => l.Segments.Sum(s => s.Qty)),
            TotalRemainingQty = t.Lines.Sum(l => l.RequestedQty - l.Segments.Sum(s => s.Qty)),
            CreatedAt = t.DocDate // Use DocDate as CreatedAt fallback
        }).ToList();

        var items = itemsData.Select(t => new TransferListItemDto(
            t.Id,
            t.SourceWarehouseId,
            warehouses.TryGetValue(t.SourceWarehouseId, out var sourceName) ? sourceName : null,
            t.DestinationWarehouseId,
            warehouses.TryGetValue(t.DestinationWarehouseId, out var destName) ? destName : null,
            Enum.IsDefined(typeof(TransferStatus), t.Status) ? t.Status.ToString() : "Draft",
            t.ExternalRef,
            t.DocDate,
            t.ShippedAt,
            t.CompletedAt,
            t.LinesCount,
            t.TotalQty,
            t.TotalAllocatedQty,
            t.TotalRemainingQty,
            t.CreatedAt
        )).ToList();

        return new TransfersListResult(
            items,
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}

