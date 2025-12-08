using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Queries;

public sealed record ReceiptDetailsQuery(Guid Id) : IRequest<ReceiptDetailsDto?>;

public sealed record ReceiptDetailsDto(
    Guid Id,
    Guid WarehouseId,
    string Status,      // Changed to string
    string Reason,      // Changed to string
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

        // Handle invalid enum values (0) that might exist in old data
        var status = Enum.IsDefined(typeof(ReceiptStatus), rec.Status) ? rec.Status.ToString() : "Draft";
        var reason = Enum.IsDefined(typeof(ReceiptReason), rec.Reason) ? rec.Reason.ToString() : "Other";

        return new ReceiptDetailsDto(
            rec.Id,
            rec.WarehouseId,
            status,
            reason,
            rec.ExternalRef,
            rec.DocDate,
            rec.ReceivedAt,
            rec.ApprovedAt,
            lines
        );
    }
}
