using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.StockLedger.Queries;

public sealed record GetStockLedgerQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    Guid? VariantId = null,
    StockMovementType? MovementType = null,
    string? RefDocType = null,
    Guid? RefDocId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<StockLedgerListResult>;

public sealed record StockLedgerListResult(
    IReadOnlyList<StockLedgerEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record StockLedgerEntryDto(
    Guid Id,
    DateTime Timestamp,
    Guid ProductId,
    Guid? VariantId,
    Guid WarehouseId,
    string? LotNumber,
    DateTime? ExpiryDate,
    decimal DeltaQty,
    decimal? UnitCost,
    StockMovementType MovementType,
    string RefDocType,
    Guid RefDocId,
    string? Note
);

public sealed class GetStockLedgerHandler : IRequestHandler<GetStockLedgerQuery, StockLedgerListResult>
{
    private readonly InventoryDbContext _db;
    public GetStockLedgerHandler(InventoryDbContext db) => _db = db;

    public async Task<StockLedgerListResult> Handle(GetStockLedgerQuery req, CancellationToken ct)
    {
        var query = _db.StockLedger.AsNoTracking().AsQueryable();

        if (req.WarehouseId.HasValue)
            query = query.Where(e => e.WarehouseId == req.WarehouseId.Value);

        if (req.ProductId.HasValue)
            query = query.Where(e => e.ProductId == req.ProductId.Value);

        if (req.VariantId.HasValue)
            query = query.Where(e => e.VariantId == req.VariantId.Value);

        if (req.MovementType.HasValue)
            query = query.Where(e => e.MovementType == req.MovementType.Value);

        if (!string.IsNullOrWhiteSpace(req.RefDocType))
            query = query.Where(e => e.RefDocType == req.RefDocType);

        if (req.RefDocId.HasValue)
            query = query.Where(e => e.RefDocId == req.RefDocId.Value);

        if (req.FromDate.HasValue)
            query = query.Where(e => e.Timestamp >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(e => e.Timestamp <= req.ToDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .ThenByDescending(e => e.Id)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        // به‌روزرسانی note برای Receipt entries بر اساس وضعیت
        var receiptIds = items
            .Where(e => e.RefDocType == "Receipt")
            .Select(e => e.RefDocId)
            .Distinct()
            .ToList();

        Dictionary<Guid, (Domain.Enums.ReceiptStatus Status, string? ExternalRef)> receiptStatuses = new();
        if (receiptIds.Count > 0)
        {
            var receiptData = await _db.Receipts
                .AsNoTracking()
                .Where(r => receiptIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Status, r.ExternalRef })
                .ToListAsync(ct);
            
            receiptStatuses = receiptData.ToDictionary(
                r => r.Id, 
                r => (r.Status, r.ExternalRef)
            );
        }

        var updatedItems = items.Select(e =>
        {
            string? updatedNote = e.Note;
            
            // به‌روزرسانی note برای Receipt entries
            if (e.RefDocType == "Receipt" && receiptStatuses.TryGetValue(e.RefDocId, out var receiptInfo))
            {
                if (receiptInfo.Status == Domain.Enums.ReceiptStatus.Approved)
                {
                    updatedNote = $"Received (Approved) - {receiptInfo.ExternalRef ?? "بدون مرجع"}";
                }
                else if (receiptInfo.Status == Domain.Enums.ReceiptStatus.Received)
                {
                    updatedNote = $"Received (Quarantine) - {receiptInfo.ExternalRef ?? "بدون مرجع"}";
                }
            }

            return new StockLedgerEntryDto(
                e.Id,
                e.Timestamp,
                e.ProductId,
                e.VariantId,
                e.WarehouseId,
                e.LotNumber,
                e.ExpiryDate,
                e.DeltaQty,
                e.UnitCost,
                e.MovementType,
                e.RefDocType,
                e.RefDocId,
                updatedNote
            );
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize);

        return new StockLedgerListResult(
            updatedItems,
            totalCount,
            req.Page,
            req.PageSize,
            totalPages
        );
    }
}

