using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Queries;

public sealed record TransferDetailsQuery(Guid Id) : IRequest<TransferDetailsDto?>;

public sealed record TransferDetailsDto(
    Guid Id,
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    string? ExternalRef,
    DateTime DocDate,
    TransferStatus Status,
    DateTime? ShippedAt,
    DateTime? CompletedAt,
    IReadOnlyList<TransferLineDto> Lines
);

public sealed record TransferLineDto(
    Guid Id,
    int LineNo,
    Guid ProductId,
    Guid? VariantId,
    decimal RequestedQty,
    decimal AllocatedQty,
    decimal RemainingQty,
    IReadOnlyList<TransferSegmentDto> Segments
);

public sealed record TransferSegmentDto(
    Guid Id,
    Guid StockItemId,
    string? Sku,
    string? LotNumber,
    DateTime? ExpiryDate,
    string? ShelfName,
    decimal Qty,
    decimal ReceivedQty,
    decimal RemainingToReceive
);

public sealed class GetTransferDetailsHandler : IRequestHandler<TransferDetailsQuery, TransferDetailsDto?>
{
    private readonly InventoryDbContext _db;
    public GetTransferDetailsHandler(InventoryDbContext db) => _db = db;

    public async Task<TransferDetailsDto?> Handle(TransferDetailsQuery req, CancellationToken ct)
    {
        var tr = await _db.Transfers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Lines)
            .ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (tr is null) return null;

        // Get all stock item IDs from segments
        var stockItemIds = tr.Lines
            .SelectMany(l => l.Segments)
            .Select(s => s.StockItemId)
            .Distinct()
            .ToList();

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
                si.ShelfId
            })
            .ToListAsync(ct);

        // Get shelf names
        var shelfIds = stockItems.Where(si => si.ShelfId.HasValue).Select(si => si.ShelfId!.Value).Distinct().ToList();
        var shelves = await _db.StockShelves
            .AsNoTracking()
            .Where(s => shelfIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var stockItemsDict = stockItems.ToDictionary(si => si.Id, si => new
        {
            si.Sku,
            si.LotNumber,
            si.ExpiryDate,
            ShelfName = si.ShelfId.HasValue && shelves.TryGetValue(si.ShelfId.Value, out var name) ? name : null
        });

        var lines = tr.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new TransferLineDto(
                l.Id,
                l.LineNo,
                l.ProductId,
                l.VariantId,
                l.RequestedQty,
                l.AllocatedQty,
                l.RemainingQty,
                l.Segments
                    .Select(s =>
                    {
                        var stockInfo = stockItemsDict.TryGetValue(s.StockItemId, out var info) ? info : null;
                        return new TransferSegmentDto(
                            s.Id,
                            s.StockItemId,
                            stockInfo?.Sku,
                            stockInfo?.LotNumber,
                            stockInfo?.ExpiryDate,
                            stockInfo?.ShelfName,
                            s.Qty,
                            s.ReceivedQty,
                            s.RemainingToReceive
                        );
                    })
                    .ToList()
            ))
            .ToList();

        return new TransferDetailsDto(
            tr.Id,
            tr.SourceWarehouseId,
            tr.DestinationWarehouseId,
            tr.ExternalRef,
            tr.DocDate,
            tr.Status,
            tr.ShippedAt,
            tr.CompletedAt,
            lines
        );
    }
}


