using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Queries;

public sealed record GetTransferLineAvailableSerialsQuery(Guid TransferId, Guid LineId)
    : IRequest<IReadOnlyList<TransferLineAvailableSerialDto>>;

public sealed record TransferLineAvailableSerialDto(
    string SerialNumber,
    Guid StockItemId,
    string? Sku,
    string? LotNumber,
    DateTime? ExpiryDate,
    Guid? ShelfId,
    string? ShelfName
);

public sealed class GetTransferLineAvailableSerialsHandler
    : IRequestHandler<GetTransferLineAvailableSerialsQuery, IReadOnlyList<TransferLineAvailableSerialDto>>
{
    private readonly IInventoryDbContext _db;
    public GetTransferLineAvailableSerialsHandler(IInventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<TransferLineAvailableSerialDto>> Handle(GetTransferLineAvailableSerialsQuery req, CancellationToken ct)
    {
        if (req.LineId == Guid.Empty) return Array.Empty<TransferLineAvailableSerialDto>();

        var transfer = await _db.Transfers
            .AsNoTracking()
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct);

        if (transfer is null)
            throw new InvalidOperationException("Transfer not found.");

        var line = transfer.Lines.FirstOrDefault(l => l.Id == req.LineId);
        if (line is null)
            throw new InvalidOperationException("Transfer line not found.");

        var stockItemsQuery = _db.StockItems
            .AsNoTracking()
            .Where(si => si.WarehouseId == transfer.SourceWarehouseId &&
                         si.ProductId == line.ProductId);

        if (line.VariantId.HasValue)
            stockItemsQuery = stockItemsQuery.Where(si => si.VariantId == line.VariantId.Value);
        else
            stockItemsQuery = stockItemsQuery.Where(si => si.VariantId == null);

        var stockItems = await stockItemsQuery
            .Select(si => new
            {
                si.Id,
                si.Sku,
                si.LotNumber,
                si.ExpiryDate,
                si.ShelfId
            })
            .ToListAsync(ct);

        if (stockItems.Count == 0) return Array.Empty<TransferLineAvailableSerialDto>();

        var stockItemIds = stockItems.Select(si => si.Id).ToList();

        var serials = await _db.StockItemSerials
            .AsNoTracking()
            .Where(s => s.StockItemId != null &&
                        stockItemIds.Contains(s.StockItemId.Value) &&
                        (s.Status == StockSerialStatus.Available ||
                         (s.Status == StockSerialStatus.Reserved && s.TransferLineId == req.LineId)))
            .OrderBy(s => s.SerialNumber)
            .Select(s => new
            {
                s.SerialNumber,
                StockItemId = s.StockItemId!.Value
            })
            .ToListAsync(ct);

        if (serials.Count == 0) return Array.Empty<TransferLineAvailableSerialDto>();

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
            si.ShelfId,
            ShelfName = si.ShelfId.HasValue && shelves.TryGetValue(si.ShelfId.Value, out var shelfName) ? shelfName : null
        });

        var result = new List<TransferLineAvailableSerialDto>(serials.Count);
        foreach (var serial in serials)
        {
            if (!stockInfo.TryGetValue(serial.StockItemId, out var info))
                continue;

            result.Add(new TransferLineAvailableSerialDto(
                serial.SerialNumber,
                serial.StockItemId,
                info.Sku,
                info.LotNumber,
                info.ExpiryDate,
                info.ShelfId,
                info.ShelfName
            ));
        }

        return result;
    }
}
