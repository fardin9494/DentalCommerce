using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
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
    decimal RemainingToReceive,
    IReadOnlyList<TransferSegmentSerialDto> Serials
);

public sealed record TransferSegmentSerialDto(
    string SerialNumber,
    StockSerialStatus Status
);

public sealed class GetTransferDetailsHandler : IRequestHandler<TransferDetailsQuery, TransferDetailsDto?>
{
    private readonly IInventoryDbContext _db;
    public GetTransferDetailsHandler(IInventoryDbContext db) => _db = db;

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

        var segmentIds = tr.Lines
            .SelectMany(l => l.Segments)
            .Select(s => s.Id)
            .ToList();

        var serialRows = new List<(Guid SegmentId, TransferSegmentSerialDto Serial)>();
        if (segmentIds.Count > 0)
        {
            var rawSerials = await _db.StockItemSerials
                .AsNoTracking()
                .Where(s => s.TransferSegmentId != null && segmentIds.Contains(s.TransferSegmentId.Value))
                .Select(s => new
                {
                    SegmentId = s.TransferSegmentId!.Value,
                    Serial = new TransferSegmentSerialDto(s.SerialNumber, s.Status)
                })
                .ToListAsync(ct);

            serialRows = rawSerials
                .Select(x => (x.SegmentId, x.Serial))
                .ToList();
        }

        var serialsLookup = serialRows
            .GroupBy(s => s.SegmentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Serial.SerialNumber)
                      .Select(x => x.Serial)
                      .ToList() as IReadOnlyList<TransferSegmentSerialDto>);

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
                        var segmentSerials = serialsLookup.TryGetValue(s.Id, out var list)
                            ? list
                            : Array.Empty<TransferSegmentSerialDto>();
                        return new TransferSegmentDto(
                            s.Id,
                            s.StockItemId,
                            stockInfo?.Sku,
                            stockInfo?.LotNumber,
                            stockInfo?.ExpiryDate,
                            stockInfo?.ShelfName,
                            s.Qty,
                            s.ReceivedQty,
                            s.RemainingToReceive,
                            segmentSerials
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


