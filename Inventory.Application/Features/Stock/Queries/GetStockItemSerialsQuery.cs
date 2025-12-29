using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Queries;

public sealed record GetStockItemSerialsQuery(Guid StockItemId, StockSerialStatus? Status = null)
    : IRequest<IReadOnlyList<StockItemSerialDto>>;

public sealed record StockItemSerialDto(
    string SerialNumber,
    StockSerialStatus Status
);

public sealed class GetStockItemSerialsHandler : IRequestHandler<GetStockItemSerialsQuery, IReadOnlyList<StockItemSerialDto>>
{
    private readonly IInventoryDbContext _db;
    public GetStockItemSerialsHandler(IInventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockItemSerialDto>> Handle(GetStockItemSerialsQuery req, CancellationToken ct)
    {
        if (req.StockItemId == Guid.Empty) return Array.Empty<StockItemSerialDto>();

        var query = _db.StockItemSerials
            .AsNoTracking()
            .Where(s => s.StockItemId == req.StockItemId);

        if (req.Status.HasValue)
            query = query.Where(s => s.Status == req.Status.Value);

        return await query
            .OrderBy(s => s.SerialNumber)
            .Select(s => new StockItemSerialDto(s.SerialNumber, s.Status))
            .ToListAsync(ct);
    }
}
