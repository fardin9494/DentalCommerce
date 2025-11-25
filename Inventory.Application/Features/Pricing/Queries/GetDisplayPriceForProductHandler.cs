using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Pricing.Queries;

public sealed class GetDisplayPriceForProductHandler
    : IRequestHandler<GetDisplayPriceForProductQuery, DisplayPriceDto>
{
    private readonly InventoryDbContext _db;
    public GetDisplayPriceForProductHandler(InventoryDbContext db) => _db = db;

    public async Task<DisplayPriceDto> Handle(GetDisplayPriceForProductQuery req, CancellationToken ct)
    {
        var now = DateTime.SpecifyKind(req.AtUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        var q =
            from si in _db.StockItems.AsNoTracking()
            where si.ProductId == req.ProductId
                  && (req.VariantId == null || si.VariantId == req.VariantId)
                  && (req.WarehouseId == null || si.WarehouseId == req.WarehouseId)
                  && (si.OnHand - si.Reserved - si.Blocked) > 0
            join sp in _db.StockItemPrices.AsNoTracking()
                on si.Id equals sp.StockItemId
            where sp.EffectiveFrom <= now && (sp.EffectiveTo == null || now < sp.EffectiveTo)
            select new { si.Id, sp.Amount, sp.Currency };

        var best = await q.OrderBy(x => x.Amount).FirstOrDefaultAsync(ct);
        if (best is null) throw new InvalidOperationException("No price for available stock.");

        return new DisplayPriceDto(best.Amount, best.Currency, best.Id);
    }
}