using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Queries;

public sealed record ReceiptDetailsQuery(Guid Id) : IRequest<ReceiptDetailsDto?>;

public sealed record ReceiptDetailsDto(
    Guid Id,
    Guid WarehouseId,
    ReceiptStatus Status,
    ReceiptReason Reason,
    string? ExternalRef,
    DateTime DocDate,
    DateTime? ReceivedAt,
    DateTime? ApprovedAt,
    IReadOnlyList<ReceiptLineDto> Lines
);

public sealed record ReceiptLineDto(
    Guid Id,
    int LineNo,
    Guid ProductId,
    Guid? VariantId,
    decimal Qty,
    string? LotNumber,
    DateTime? ExpiryDateUtc,
    decimal? UnitCost
);

public sealed class GetReceiptDetailsHandler : IRequestHandler<ReceiptDetailsQuery, ReceiptDetailsDto?>
{
    private readonly InventoryDbContext _db;
    public GetReceiptDetailsHandler(InventoryDbContext db) => _db = db;

    public async Task<ReceiptDetailsDto?> Handle(ReceiptDetailsQuery req, CancellationToken ct)
    {
        var rec = await _db.Receipts
            .AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);

        if (rec is null) return null;

        var lines = rec.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new ReceiptLineDto(
                l.Id,
                l.LineNo,
                l.ProductId,
                l.VariantId,
                l.Qty,
                l.LotNumber,
                l.ExpiryDate,
                l.UnitCost
            ))
            .ToList();

        return new ReceiptDetailsDto(
            rec.Id,
            rec.WarehouseId,
            rec.Status,
            rec.Reason,
            rec.ExternalRef,
            rec.DocDate,
            rec.ReceivedAt,
            rec.ApprovedAt,
            lines
        );
    }
}


