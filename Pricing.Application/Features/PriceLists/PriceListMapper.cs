using Pricing.Domain.PriceLists;

namespace Pricing.Application.Features.PriceLists;

internal static class PriceListMapper
{
    public static PriceListDto Map(PriceList list)
    {
        return new PriceListDto
        {
            Id = list.Id,
            Name = list.Name,
            Currency = list.Currency,
            ValidFrom = list.ValidFrom,
            ValidTo = list.ValidTo,
            IsActive = list.IsActive,
            Items = list.Items.Select(i => new PriceListItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                BasePrice = i.BasePrice,
                TierPrices = i.TierPrices.Select(t => new TierPriceDto
                {
                    MinQty = t.MinQty,
                    UnitPrice = t.UnitPrice
                }).ToList()
            }).ToList()
        };
    }
}
