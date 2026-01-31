using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.PriceLists;

namespace Pricing.Application.Features.Bulk.Commands;

public sealed record BulkUpdatePricesCommand(
    Guid? PriceListId,
    string Currency,
    IReadOnlyList<BulkPriceItem> Items) : IRequest<int>;

public sealed record BulkPriceItem(
    string SkuId,
    decimal BasePrice,
    IReadOnlyList<BulkTierPrice>? TierPrices);

public sealed record BulkTierPrice(int MinQty, decimal UnitPrice);

public sealed class BulkUpdatePricesHandler : IRequestHandler<BulkUpdatePricesCommand, int>
{
    private readonly Abstractions.IPricingDbContext _db;
    private readonly Abstractions.ITransactionRunner _tx;

    public BulkUpdatePricesHandler(Abstractions.IPricingDbContext db, Abstractions.ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<int> Handle(BulkUpdatePricesCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0) return 0;

        return await _tx.ExecuteAsync(async token =>
        {
            var priceList = request.PriceListId.HasValue
                ? await _db.PriceLists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PriceListId.Value, token)
                : await _db.PriceLists.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive, token);

            if (priceList is null)
                throw new InvalidOperationException("Active price list not found.");

            var updated = 0;
            var hasTrackedChanges = false;

            if (!string.IsNullOrWhiteSpace(request.Currency) && !string.Equals(priceList.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            {
                await _db.PriceLists
                    .Where(x => x.Id == priceList.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Currency, request.Currency.Trim().ToUpperInvariant())
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), token);
            }

            foreach (var item in request.Items)
            {
                var sku = item.SkuId.Trim();
                var tiers = item.TierPrices?.Select(t => new TierPrice(t.MinQty, t.UnitPrice)).ToList();

                if (tiers is null || tiers.Count == 0)
                {
                    var affected = await _db.PriceListItems
                        .Where(x => x.PriceListId == priceList.Id && x.SkuId == sku)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.BasePrice, item.BasePrice)
                            .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), token);

                    if (affected == 0)
                    {
                        _db.PriceListItems.Add(PriceListItem.Create(priceList.Id, sku, item.BasePrice, null));
                        hasTrackedChanges = true;
                    }

                    updated++;
                    continue;
                }

                var existing = await _db.PriceListItems
                    .FirstOrDefaultAsync(x => x.PriceListId == priceList.Id && x.SkuId == sku, token);

                if (existing is null)
                {
                    _db.PriceListItems.Add(PriceListItem.Create(priceList.Id, sku, item.BasePrice, tiers));
                }
                else
                {
                    existing.Update(item.BasePrice, tiers);
                }

                hasTrackedChanges = true;
                updated++;
            }

            if (hasTrackedChanges)
            {
                await _db.SaveChangesAsync(token);
            }

            return updated;
        }, ct);
    }
}
