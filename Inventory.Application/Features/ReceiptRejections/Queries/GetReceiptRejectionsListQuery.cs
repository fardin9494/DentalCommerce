using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.ReceiptRejections.Queries;

public sealed record GetReceiptRejectionsListQuery(
    Guid? WarehouseId = null,
    ReceiptRejectionStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<ReceiptRejectionsListResult>;

public sealed record ReceiptRejectionsListResult(
    IReadOnlyList<ReceiptRejectionListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record ReceiptRejectionListItemDto(
    Guid ReceiptLineId,
    Guid ReceiptId,
    Guid WarehouseId,
    string? WarehouseName,
    string ReceiptStatus,
    string ReceiptReason,
    string? ReceiptExternalRef,
    DateTime ReceiptDocDate,
    int LineNo,
    Guid ProductId,
    Guid? VariantId,
    string? LotNumber,
    DateTime? ExpiryDateUtc,
    decimal? UnitCost,
    decimal RejectedQty,
    decimal RejectionApprovedQty,
    decimal RejectionReturnedQty,
    decimal RejectionDisposedQty,
    string? RejectionReason,
    string RejectionStatus,
    DateTime? RejectionResolvedAt,
    string? RejectionResolutionNote,
    DateTime CreatedAt
);

public sealed class GetReceiptRejectionsListHandler : IRequestHandler<GetReceiptRejectionsListQuery, ReceiptRejectionsListResult>
{
    private readonly InventoryDbContext _db;
    public GetReceiptRejectionsListHandler(InventoryDbContext db) => _db = db;

    public async Task<ReceiptRejectionsListResult> Handle(GetReceiptRejectionsListQuery req, CancellationToken ct)
    {
        var query = from line in _db.ReceiptLines.AsNoTracking()
                    join receipt in _db.Receipts.AsNoTracking() on line.ReceiptId equals receipt.Id
                    where line.RejectedQty > 0
                    select new { Line = line, Receipt = receipt };

        if (req.WarehouseId.HasValue)
            query = query.Where(x => x.Receipt.WarehouseId == req.WarehouseId.Value);

        if (req.Status.HasValue)
            query = query.Where(x => x.Line.RejectionStatus == req.Status.Value);

        if (req.FromDate.HasValue)
            query = query.Where(x => x.Receipt.DocDate >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(x => x.Receipt.DocDate <= req.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Receipt.ExternalRef != null && x.Receipt.ExternalRef.ToLower().Contains(term)) ||
                x.Receipt.Id.ToString().ToLower().Contains(term) ||
                x.Line.Id.ToString().ToLower().Contains(term) ||
                x.Line.ProductId.ToString().ToLower().Contains(term) ||
                (x.Line.LotNumber != null && x.Line.LotNumber.ToLower().Contains(term))
            );
        }

        var totalCount = await query.CountAsync(ct);
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        var warehouseIds = await query.Select(x => x.Receipt.WarehouseId).Distinct().ToListAsync(ct);
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        var rawItems = await query
            .OrderByDescending(x => x.Receipt.DocDate)
            .ThenByDescending(x => x.Line.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                ReceiptLineId = x.Line.Id,
                x.Line.LineNo,
                x.Line.ProductId,
                x.Line.VariantId,
                x.Line.LotNumber,
                x.Line.ExpiryDate,
                x.Line.UnitCost,
                x.Line.RejectedQty,
                x.Line.RejectionApprovedQty,
                x.Line.RejectionReturnedQty,
                x.Line.RejectionDisposedQty,
                x.Line.RejectionReason,
                x.Line.RejectionStatus,
                x.Line.RejectionResolvedAt,
                x.Line.RejectionResolutionNote,
                x.Line.CreatedAt,
                ReceiptId = x.Receipt.Id,
                x.Receipt.WarehouseId,
                x.Receipt.Status,
                x.Receipt.Reason,
                x.Receipt.ExternalRef,
                x.Receipt.DocDate
            })
            .ToListAsync(ct);

        var items = rawItems.Select(x =>
        {
            var receiptStatus = Enum.IsDefined(typeof(ReceiptStatus), x.Status) ? x.Status.ToString() : "Draft";
            var receiptReason = Enum.IsDefined(typeof(ReceiptReason), x.Reason) ? x.Reason.ToString() : "Other";
            var rejectionStatus = Enum.IsDefined(typeof(ReceiptRejectionStatus), x.RejectionStatus)
                ? x.RejectionStatus.ToString()
                : "Pending";

            if (x.RejectionStatus == ReceiptRejectionStatus.None)
                rejectionStatus = "Pending";

            return new ReceiptRejectionListItemDto(
                x.ReceiptLineId,
                x.ReceiptId,
                x.WarehouseId,
                warehouses.TryGetValue(x.WarehouseId, out var name) ? name : null,
                receiptStatus,
                receiptReason,
                x.ExternalRef,
                x.DocDate,
                x.LineNo,
                x.ProductId,
                x.VariantId,
                x.LotNumber,
                x.ExpiryDate,
                x.UnitCost,
                x.RejectedQty,
                x.RejectionApprovedQty,
                x.RejectionReturnedQty,
                x.RejectionDisposedQty,
                x.RejectionReason,
                rejectionStatus,
                x.RejectionResolvedAt,
                x.RejectionResolutionNote,
                x.CreatedAt
            );
        }).ToList();

        return new ReceiptRejectionsListResult(
            items,
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}
