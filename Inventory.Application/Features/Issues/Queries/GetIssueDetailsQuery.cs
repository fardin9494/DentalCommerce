using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Queries;

public sealed record IssueDetailsQuery(Guid Id) : IRequest<IssueDetailsDto?>;

public sealed record IssueDetailsDto(
    Guid Id,
    Guid? WarehouseId,
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
    string? ShelfName,
    Guid? WarehouseId, // Changed to nullable
    string? WarehouseName,
    IReadOnlyList<IssueAllocationSerialDto> Serials
);

public sealed record IssueAllocationSerialDto(
    string SerialNumber,
    StockSerialStatus Status
);

public sealed class GetIssueDetailsHandler : IRequestHandler<IssueDetailsQuery, IssueDetailsDto?>
{
    private readonly InventoryDbContext _db;
    public GetIssueDetailsHandler(InventoryDbContext db) => _db = db;

    public async Task<IssueDetailsDto?> Handle(IssueDetailsQuery req, CancellationToken ct)
    {
        var issue = await _db.Issues
            .AsNoTracking()
            .AsSplitQuery()
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

        if (stockItemIds.Count == 0)
        {
            // اگر تخصیصی وجود ندارد، خطوط را بدون تخصیص برمی‌گردانیم
            var emptyLines = issue.Lines
                .OrderBy(l => l.LineNo)
                .Select(l => new IssueLineDto(
                    l.Id,
                    l.LineNo,
                    l.ProductId,
                    l.VariantId,
                    l.RequestedQty,
                    l.AllocatedQty,
                    l.RemainingQty,
                    Array.Empty<IssueAllocationDto>()
                ))
                .ToList();

            return new IssueDetailsDto(
                issue.Id,
                issue.WarehouseId,
                issue.Status,
                issue.ExternalRef,
                issue.DocDate,
                issue.PostedAt,
                emptyLines
            );
        }

        var lineIds = issue.Lines.Select(l => l.Id).ToList();

        var serials = await _db.StockItemSerials
            .AsNoTracking()
            .Where(s => s.IssueLineId != null &&
                        lineIds.Contains(s.IssueLineId.Value) &&
                        s.StockItemId != null &&
                        (s.Status == StockSerialStatus.Reserved || s.Status == StockSerialStatus.Issued))
            .Select(s => new
            {
                IssueLineId = s.IssueLineId!.Value,
                StockItemId = s.StockItemId!.Value,
                s.SerialNumber,
                s.Status
            })
            .ToListAsync(ct);

        var serialsLookup = serials
            .GroupBy(s => (s.IssueLineId, s.StockItemId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.SerialNumber)
                      .Select(x => new IssueAllocationSerialDto(x.SerialNumber, x.Status))
                      .ToList() as IReadOnlyList<IssueAllocationSerialDto>);

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
                si.ShelfId,
                si.WarehouseId
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

        // Get warehouse names
        var warehouseIds = stockItems.Select(si => si.WarehouseId).Distinct().ToList();
        var warehouses = warehouseIds.Count > 0
            ? await _db.Warehouses
                .AsNoTracking()
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name, ct)
            : new Dictionary<Guid, string>();

        var stockItemsDict = stockItems.ToDictionary(si => si.Id, si => new
        {
            si.Sku,
            si.LotNumber,
            si.ExpiryDate,
            si.ShelfId,
            si.WarehouseId,
            ShelfName = si.ShelfId.HasValue && shelves.TryGetValue(si.ShelfId.Value, out var shelfName) ? shelfName : null,
            WarehouseName = warehouses.TryGetValue(si.WarehouseId, out var warehouseName) ? warehouseName : null
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
                        var serialKey = (l.Id, a.StockItemId);
                        var serialsForAlloc = serialsLookup.TryGetValue(serialKey, out var serialList)
                            ? serialList
                            : Array.Empty<IssueAllocationSerialDto>();
                        if (!stockItemsDict.TryGetValue(a.StockItemId, out var stockInfo))
                        {
                            // اگر StockItem پیدا نشد، اطلاعات محدود برمی‌گردانیم
                            return new IssueAllocationDto(
                                a.Id,
                                a.StockItemId,
                                a.Qty,
                                null, // Sku
                                null, // LotNumber
                                null, // ExpiryDate
                                null, // ShelfId
                                null, // ShelfName
                                null, // WarehouseId (null instead of Guid.Empty)
                                null, // WarehouseName
                                serialsForAlloc
                            );
                        }

                        return new IssueAllocationDto(
                            a.Id,
                            a.StockItemId,
                            a.Qty,
                            stockInfo.Sku,
                            stockInfo.LotNumber,
                            stockInfo.ExpiryDate,
                            stockInfo.ShelfId,
                            stockInfo.ShelfName,
                            stockInfo.WarehouseId, // Already nullable
                            stockInfo.WarehouseName,
                            serialsForAlloc
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
