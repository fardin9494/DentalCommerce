using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Pricing.Commands;

public sealed class SetStockItemPriceHandler : IRequestHandler<SetStockItemPriceCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public SetStockItemPriceHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(SetStockItemPriceCommand req, CancellationToken ct)
    {
        // وجود StockItem
        var stockItemExists = await _db.StockItems.AnyAsync(x => x.Id == req.StockItemId, ct);
        if (!stockItemExists) throw new InvalidOperationException("StockItem not found.");

        var from = DateTime.SpecifyKind(req.EffectiveFrom ?? DateTime.UtcNow, DateTimeKind.Utc);

        // اگر قیمت فعالی در این لحظه هست، ببندش تا از from به بعد رکورد جدید فعال شود.
        var active = await _db.StockItemPrices
            .Where(p => p.StockItemId == req.StockItemId
                        && p.EffectiveFrom <= from
                        && (p.EffectiveTo == null || from < p.EffectiveTo))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (active is not null)
        {
            // بستن رکورد قبلی در لحظه‌ی from (exclusive end)
            active.CloseAt(from);
        }

        var price = StockItemPrice.Create(req.StockItemId, req.Amount, req.Currency, from, req.EffectiveTo);
        _db.StockItemPrices.Add(price);

        await _db.SaveChangesAsync(ct);
        return price.Id;
    }
}