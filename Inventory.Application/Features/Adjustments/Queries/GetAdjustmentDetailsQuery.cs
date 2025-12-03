using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Queries;

public sealed record AdjustmentDetailsQuery(Guid Id) : IRequest<AdjustmentDetailsDto?>;

public sealed record AdjustmentDetailsDto(
    Guid Id,
    Guid WarehouseId,
    AdjustmentStatus Status,
    AdjustmentReason Reason,
    string? Note,
    DateTime DocDate,
    DateTime? PostedAt,
    IReadOnlyList<AdjustmentLineDto> Lines
);

public sealed record AdjustmentLineDto(
    Guid Id,
    int LineNo,
    Guid ProductId,
    Guid? VariantId,
    string? LotNumber,
    DateTime? ExpiryDateUtc,
    decimal QtyDelta
);

public sealed class GetAdjustmentDetailsHandler : IRequestHandler<AdjustmentDetailsQuery, AdjustmentDetailsDto?>
{
    private readonly InventoryDbContext _db;
    public GetAdjustmentDetailsHandler(InventoryDbContext db) => _db = db;

    public async Task<AdjustmentDetailsDto?> Handle(AdjustmentDetailsQuery req, CancellationToken ct)
    {
        var adj = await _db.Adjustments
            .AsNoTracking()
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct);

        if (adj is null) return null;

        var lines = adj.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new AdjustmentLineDto(
                l.Id,
                l.LineNo,
                l.ProductId,
                l.VariantId,
                l.LotNumber,
                l.ExpiryDate,
                l.QtyDelta
            ))
            .ToList();

        return new AdjustmentDetailsDto(
            adj.Id,
            adj.WarehouseId,
            adj.Status,
            adj.Reason,
            adj.Note,
            adj.DocDate,
            adj.PostedAt,
            lines
        );
    }
}


