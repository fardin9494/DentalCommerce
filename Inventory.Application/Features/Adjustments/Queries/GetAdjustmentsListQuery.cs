using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Queries;

public sealed record GetAdjustmentsListQuery(
    Guid? WarehouseId = null,
    AdjustmentStatus? Status = null,
    AdjustmentReason? Reason = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<AdjustmentsListResult>;

public sealed record AdjustmentsListResult(
    IReadOnlyList<AdjustmentListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record AdjustmentListItemDto(
    Guid Id,
    Guid WarehouseId,
    string? WarehouseName,
    string Status,      // Changed to string
    string Reason,      // Changed to string
    string? Note,
    DateTime DocDate,
    DateTime? PostedAt,
    int LinesCount,
    decimal TotalQtyDelta,  // Sum of all QtyDelta (can be positive or negative)
    DateTime CreatedAt
);

public sealed class GetAdjustmentsListHandler : IRequestHandler<GetAdjustmentsListQuery, AdjustmentsListResult>
{
    private readonly IInventoryDbContext _db;
    public GetAdjustmentsListHandler(IInventoryDbContext db) => _db = db;

    public async Task<AdjustmentsListResult> Handle(GetAdjustmentsListQuery req, CancellationToken ct)
    {
        var query = _db.Adjustments
            .AsNoTracking()
            .Include(a => a.Lines)
            .AsQueryable();

        // Apply filters
        if (req.WarehouseId.HasValue)
            query = query.Where(a => a.WarehouseId == req.WarehouseId.Value);

        if (req.Status.HasValue)
            query = query.Where(a => a.Status == req.Status.Value);

        if (req.Reason.HasValue)
            query = query.Where(a => a.Reason == req.Reason.Value);

        if (req.FromDate.HasValue)
            query = query.Where(a => a.DocDate >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(a => a.DocDate <= req.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchTerm = req.Search.Trim().ToLower();
            query = query.Where(a => 
                (a.Note != null && a.Note.ToLower().Contains(searchTerm)) ||
                a.Id.ToString().ToLower().Contains(searchTerm)
            );
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Calculate pagination
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        // Get warehouses for names
        var warehouseIds = await query.Select(a => a.WarehouseId).Distinct().ToListAsync(ct);
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        // Get paginated results - fetch raw data first
        var rawItems = await query
            .OrderByDescending(a => a.DocDate)
            .ThenByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.WarehouseId,
                a.Status,
                a.Reason,
                a.Note,
                a.DocDate,
                a.PostedAt,
                LinesCount = a.Lines.Count,
                TotalQtyDelta = a.Lines.Sum(l => l.QtyDelta),
                a.CreatedAt
            })
            .ToListAsync(ct);

        // Convert to DTOs with string enums and warehouse names
        // Handle invalid enum values (0) that might exist in old data
        var items = rawItems.Select(a => new AdjustmentListItemDto(
            a.Id,
            a.WarehouseId,
            warehouses.TryGetValue(a.WarehouseId, out var name) ? name : null,
            Enum.IsDefined(typeof(AdjustmentStatus), a.Status) ? a.Status.ToString() : "Draft",
            Enum.IsDefined(typeof(AdjustmentReason), a.Reason) ? a.Reason.ToString() : "Other",
            a.Note,
            a.DocDate,
            a.PostedAt,
            a.LinesCount,
            a.TotalQtyDelta,
            a.CreatedAt
        )).ToList();

        return new AdjustmentsListResult(
            items,
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}

