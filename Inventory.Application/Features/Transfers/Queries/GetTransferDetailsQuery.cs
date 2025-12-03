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
    decimal Qty,
    decimal ReceivedQty
);

public sealed class GetTransferDetailsHandler : IRequestHandler<TransferDetailsQuery, TransferDetailsDto?>
{
    private readonly InventoryDbContext _db;
    public GetTransferDetailsHandler(InventoryDbContext db) => _db = db;

    public async Task<TransferDetailsDto?> Handle(TransferDetailsQuery req, CancellationToken ct)
    {
        var tr = await _db.Transfers
            .AsNoTracking()
            .Include(t => t.Lines)
            .ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (tr is null) return null;

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
                    .Select(s => new TransferSegmentDto(s.Id, s.StockItemId, s.Qty, s.ReceivedQty))
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


