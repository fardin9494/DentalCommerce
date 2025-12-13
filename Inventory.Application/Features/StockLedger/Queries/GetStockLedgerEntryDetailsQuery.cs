using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.StockLedger.Queries;

public sealed record GetStockLedgerEntryDetailsQuery(Guid Id) : IRequest<StockLedgerEntryDetailsDto?>;

public sealed record StockLedgerEntryDetailsDto(
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
    string? Note,
    string? WarehouseName,
    string? ProductName,
    string? VariantName,
    string? Sku,
    object? RefDocDetails // جزئیات سند مرجع (Receipt, Issue, Transfer, Adjustment)
);

public sealed class GetStockLedgerEntryDetailsHandler : IRequestHandler<GetStockLedgerEntryDetailsQuery, StockLedgerEntryDetailsDto?>
{
    private readonly InventoryDbContext _db;
    public GetStockLedgerEntryDetailsHandler(InventoryDbContext db) => _db = db;

    public async Task<StockLedgerEntryDetailsDto?> Handle(GetStockLedgerEntryDetailsQuery req, CancellationToken ct)
    {
        var entry = await _db.StockLedger
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct);

        if (entry is null) return null;

        // دریافت نام انبار
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == entry.WarehouseId)
            .Select(w => w.Name)
            .FirstOrDefaultAsync(ct);

        // دریافت اطلاعات محصول از Catalog (اگر نیاز باشد می‌توان از Gateway استفاده کرد)
        // فعلاً فقط SKU را از StockItem می‌گیریم
        var stockItem = await _db.StockItems
            .AsNoTracking()
            .Where(si => si.ProductId == entry.ProductId &&
                        si.VariantId == entry.VariantId &&
                        si.WarehouseId == entry.WarehouseId &&
                        si.LotNumber == entry.LotNumber &&
                        si.ExpiryDate == entry.ExpiryDate)
            .Select(si => new { si.Sku })
            .FirstOrDefaultAsync(ct);

        // دریافت جزئیات سند مرجع و به‌روزرسانی note بر اساس وضعیت
        object? refDocDetails = null;
        string? updatedNote = entry.Note;
        
        switch (entry.RefDocType)
        {
            case "Receipt":
                var receipt = await _db.Receipts
                    .AsNoTracking()
                    .Include(r => r.Lines)
                    .Where(r => r.Id == entry.RefDocId)
                    .Select(r => new
                    {
                        r.Id,
                        r.WarehouseId,
                        r.ExternalRef,
                        r.DocDate,
                        r.Status,
                        r.ReceivedAt,
                        r.ApprovedAt,
                        Lines = r.Lines.Select(l => new
                        {
                            l.Id,
                            l.LineNo,
                            l.ProductId,
                            l.VariantId,
                            l.Qty,
                            l.LotNumber,
                            l.ExpiryDate,
                            l.UnitCost,
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(ct);
                refDocDetails = receipt;
                
                // به‌روزرسانی note بر اساس وضعیت Receipt
                if (receipt != null)
                {
                    if (receipt.Status == Domain.Enums.ReceiptStatus.Approved)
                    {
                        updatedNote = $"Received (Approved) - {receipt.ExternalRef ?? "بدون مرجع"}";
                    }
                    else if (receipt.Status == Domain.Enums.ReceiptStatus.Received)
                    {
                        updatedNote = $"Received (Quarantine) - {receipt.ExternalRef ?? "بدون مرجع"}";
                    }
                }
                break;

            case "Issue":
                var issue = await _db.Issues
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(i => i.Lines)
                    .ThenInclude(l => l.Allocations)
                    .Where(i => i.Id == entry.RefDocId)
                    .Select(i => new
                    {
                        i.Id,
                        i.WarehouseId,
                        i.ExternalRef,
                        i.DocDate,
                        i.Status,
                        i.PostedAt,
                        Lines = i.Lines.Select(l => new
                        {
                            l.Id,
                            l.LineNo,
                            l.ProductId,
                            l.VariantId,
                            l.RequestedQty,
                            Allocations = l.Allocations.Select(a => new
                            {
                                a.Id,
                                a.StockItemId,
                                a.Qty,
                            }).ToList()
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(ct);
                refDocDetails = issue;
                break;

            case "Transfer":
                var transfer = await _db.Transfers
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(t => t.Lines)
                    .ThenInclude(l => l.Segments)
                    .Where(t => t.Id == entry.RefDocId)
                    .Select(t => new
                    {
                        t.Id,
                        t.SourceWarehouseId,
                        t.DestinationWarehouseId,
                        t.ExternalRef,
                        t.DocDate,
                        t.Status,
                        t.ShippedAt,
                        t.CompletedAt,
                        Lines = t.Lines.Select(l => new
                        {
                            l.Id,
                            l.LineNo,
                            l.ProductId,
                            l.VariantId,
                            l.RequestedQty,
                            l.AllocatedQty,
                            Segments = l.Segments.Select(s => new
                            {
                                s.Id,
                                s.StockItemId,
                                s.Qty,
                                s.ReceivedQty,
                            }).ToList()
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(ct);
                refDocDetails = transfer;
                break;

            case "Adjustment":
                var adjustment = await _db.Adjustments
                    .AsNoTracking()
                    .Include(a => a.Lines)
                    .Where(a => a.Id == entry.RefDocId)
                    .Select(a => new
                    {
                        a.Id,
                        a.WarehouseId,
                        DocDate = a.DocDate,
                        a.Status,
                        a.PostedAt,
                        a.Reason,
                        a.Note,
                        Lines = a.Lines.Select(l => new
                        {
                            l.Id,
                            l.LineNo,
                            l.ProductId,
                            l.VariantId,
                            l.QtyDelta,
                            l.LotNumber,
                            l.ExpiryDate,
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(ct);
                refDocDetails = adjustment;
                break;

            case "StockMove":
                // برای انتقال قفسه، اطلاعات StockItem را می‌دهیم
                var stockMoveItem = await _db.StockItems
                    .AsNoTracking()
                    .Where(si => si.Id == entry.RefDocId)
                    .Select(si => new
                    {
                        si.Id,
                        si.ProductId,
                        si.VariantId,
                        si.WarehouseId,
                        si.ShelfId,
                        si.Sku,
                        si.LotNumber,
                        si.ExpiryDate,
                        si.OnHand,
                        si.Available,
                        si.Reserved,
                        si.Blocked,
                    })
                    .FirstOrDefaultAsync(ct);
                refDocDetails = stockMoveItem;
                break;
        }

        return new StockLedgerEntryDetailsDto(
            entry.Id,
            entry.Timestamp,
            entry.ProductId,
            entry.VariantId,
            entry.WarehouseId,
            entry.LotNumber,
            entry.ExpiryDate,
            entry.DeltaQty,
            entry.UnitCost,
            entry.MovementType,
            entry.RefDocType,
            entry.RefDocId,
            updatedNote ?? entry.Note, // استفاده از note به‌روزرسانی شده
            warehouse,
            null, // ProductName - می‌توان از Catalog Gateway دریافت کرد
            null, // VariantName - می‌توان از Catalog Gateway دریافت کرد
            stockItem?.Sku,
            refDocDetails
        );
    }
}

