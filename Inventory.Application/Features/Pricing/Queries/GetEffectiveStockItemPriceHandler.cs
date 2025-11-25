using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Pricing.Queries;

public sealed class GetEffectiveStockItemPriceHandler
    : IRequestHandler<GetEffectiveStockItemPriceQuery, StockItemPriceDto>
{
    private readonly InventoryDbContext _db;
    public GetEffectiveStockItemPriceHandler(InventoryDbContext db) => _db = db;

    public async Task<StockItemPriceDto> Handle(GetEffectiveStockItemPriceQuery req, CancellationToken ct)
    {
        var at = DateTime.SpecifyKind(req.AtUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        var p = await _db.StockItemPrices.AsNoTracking()
            .Where(x => x.StockItemId == req.StockItemId
                        && x.EffectiveFrom <= at
                        && (x.EffectiveTo == null || at < x.EffectiveTo))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (p is null) throw new InvalidOperationException("No active price found.");
        return new StockItemPriceDto(p.Id, p.StockItemId, p.Amount, p.Currency, p.EffectiveFrom, p.EffectiveTo);
    }
}