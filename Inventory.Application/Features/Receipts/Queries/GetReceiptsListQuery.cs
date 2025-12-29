using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Queries;

public sealed record GetReceiptsListQuery(
    Guid? WarehouseId = null,
    ReceiptStatus? Status = null,
    ReceiptReason? Reason = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<ReceiptsListResult>;

public sealed record ReceiptsListResult(
    IReadOnlyList<ReceiptListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record ReceiptListItemDto(
    Guid Id,
    Guid WarehouseId,
    string? WarehouseName,
    string Status,      // Changed to string
    string Reason,      // Changed to string
    string? ExternalRef,
    DateTime DocDate,
    DateTime? ReceivedAt,
    DateTime? ApprovedAt,
    int LinesCount,
    decimal TotalQty,
    DateTime CreatedAt
);

public sealed class GetReceiptsListHandler : IRequestHandler<GetReceiptsListQuery, ReceiptsListResult>
{
    private readonly IInventoryDbContext _db;
    public GetReceiptsListHandler(IInventoryDbContext db) => _db = db;

    public async Task<ReceiptsListResult> Handle(GetReceiptsListQuery req, CancellationToken ct)
    {
        var query = _db.Receipts
            .AsNoTracking()
            .Include(r => r.Lines)
            .AsQueryable();

        // Apply filters
        if (req.WarehouseId.HasValue)
            query = query.Where(r => r.WarehouseId == req.WarehouseId.Value);

        if (req.Status.HasValue)
            query = query.Where(r => r.Status == req.Status.Value);

        if (req.Reason.HasValue)
            query = query.Where(r => r.Reason == req.Reason.Value);

        if (req.FromDate.HasValue)
            query = query.Where(r => r.DocDate >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(r => r.DocDate <= req.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchTerm = req.Search.Trim().ToLower();
            query = query.Where(r => 
                (r.ExternalRef != null && r.ExternalRef.ToLower().Contains(searchTerm)) ||
                r.Id.ToString().ToLower().Contains(searchTerm)
            );
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Calculate pagination
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        // Get warehouses for names
        var warehouseIds = await query.Select(r => r.WarehouseId).Distinct().ToListAsync(ct);
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        // Get paginated results - fetch raw data first
        var rawItems = await query
            .OrderByDescending(r => r.DocDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.WarehouseId,
                r.Status,
                r.Reason,
                r.ExternalRef,
                r.DocDate,
                r.ReceivedAt,
                r.ApprovedAt,
                LinesCount = r.Lines.Count,
                TotalQty = r.Lines.Sum(l => l.Qty),
                r.CreatedAt
            })
            .ToListAsync(ct);

        // Convert to DTOs with string enums and warehouse names
        // Handle invalid enum values (0) that might exist in old data
        var items = rawItems.Select(r => new ReceiptListItemDto(
            r.Id,
            r.WarehouseId,
            warehouses.TryGetValue(r.WarehouseId, out var name) ? name : null,
            Enum.IsDefined(typeof(ReceiptStatus), r.Status) ? r.Status.ToString() : "Draft",
            Enum.IsDefined(typeof(ReceiptReason), r.Reason) ? r.Reason.ToString() : "Other",
            r.ExternalRef,
            r.DocDate,
            r.ReceivedAt,
            r.ApprovedAt,
            r.LinesCount,
            r.TotalQty,
            r.CreatedAt
        )).ToList();

        return new ReceiptsListResult(
            items,
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}
