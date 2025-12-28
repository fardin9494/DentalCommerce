using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Queries;

public sealed record GetIssueLineAvailableSerialsQuery(Guid IssueId, Guid LineId, Guid? WarehouseId = null)
    : IRequest<IReadOnlyList<IssueLineAvailableSerialDto>>;

public sealed record IssueLineAvailableSerialDto(
    string SerialNumber,
    Guid StockItemId,
    string? Sku,
    string? LotNumber,
    DateTime? ExpiryDate,
    Guid WarehouseId,
    string? WarehouseName,
    Guid? ShelfId,
    string? ShelfName
);

public sealed class GetIssueLineAvailableSerialsHandler
    : IRequestHandler<GetIssueLineAvailableSerialsQuery, IReadOnlyList<IssueLineAvailableSerialDto>>
{
    private readonly InventoryDbContext _db;
    public GetIssueLineAvailableSerialsHandler(InventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<IssueLineAvailableSerialDto>> Handle(GetIssueLineAvailableSerialsQuery req, CancellationToken ct)
    {
        if (req.LineId == Guid.Empty) return Array.Empty<IssueLineAvailableSerialDto>();

        var line = await _db.IssueLines
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == req.LineId && l.IssueId == req.IssueId, ct);

        if (line is null)
            throw new InvalidOperationException("خط سند خروج پیدا نشد.");

        var stockItemsQuery = _db.StockItems
            .AsNoTracking()
            .Where(si => si.ProductId == line.ProductId);

        if (line.VariantId.HasValue)
            stockItemsQuery = stockItemsQuery.Where(si => si.VariantId == line.VariantId.Value);
        else
            stockItemsQuery = stockItemsQuery.Where(si => si.VariantId == null);

        if (req.WarehouseId.HasValue)
            stockItemsQuery = stockItemsQuery.Where(si => si.WarehouseId == req.WarehouseId.Value);

        var stockItems = await stockItemsQuery
            .Select(si => new
            {
                si.Id,
                si.Sku,
                si.LotNumber,
                si.ExpiryDate,
                si.WarehouseId,
                si.ShelfId
            })
            .ToListAsync(ct);

        if (stockItems.Count == 0) return Array.Empty<IssueLineAvailableSerialDto>();

        var stockItemIds = stockItems.Select(si => si.Id).ToList();

        var serials = await _db.StockItemSerials
            .AsNoTracking()
            .Where(s => s.StockItemId != null &&
                        stockItemIds.Contains(s.StockItemId.Value) &&
                        (s.Status == StockSerialStatus.Available ||
                         (s.Status == StockSerialStatus.Reserved && s.IssueLineId == req.LineId)))
            .OrderBy(s => s.SerialNumber)
            .Select(s => new
            {
                s.SerialNumber,
                StockItemId = s.StockItemId!.Value
            })
            .ToListAsync(ct);

        if (serials.Count == 0) return Array.Empty<IssueLineAvailableSerialDto>();

        var warehouseIds = stockItems.Select(si => si.WarehouseId).Distinct().ToList();
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        var shelfIds = stockItems.Where(si => si.ShelfId.HasValue).Select(si => si.ShelfId!.Value).Distinct().ToList();
        var shelves = shelfIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.StockShelves
                .AsNoTracking()
                .Where(s => shelfIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var stockInfo = stockItems.ToDictionary(si => si.Id, si => new
        {
            si.Sku,
            si.LotNumber,
            si.ExpiryDate,
            si.WarehouseId,
            si.ShelfId,
            WarehouseName = warehouses.TryGetValue(si.WarehouseId, out var whName) ? whName : null,
            ShelfName = si.ShelfId.HasValue && shelves.TryGetValue(si.ShelfId.Value, out var shelfName) ? shelfName : null
        });

        var result = new List<IssueLineAvailableSerialDto>(serials.Count);
        foreach (var serial in serials)
        {
            if (!stockInfo.TryGetValue(serial.StockItemId, out var info))
                continue;

            result.Add(new IssueLineAvailableSerialDto(
                serial.SerialNumber,
                serial.StockItemId,
                info.Sku,
                info.LotNumber,
                info.ExpiryDate,
                info.WarehouseId,
                info.WarehouseName,
                info.ShelfId,
                info.ShelfName
            ));
        }

        return result;
    }
}
