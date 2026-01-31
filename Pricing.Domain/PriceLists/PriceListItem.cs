using BuildingBlocks.Domain;

namespace Pricing.Domain.PriceLists;

public sealed class PriceListItem : BaseEntity<Guid>
{
    public Guid PriceListId { get; private set; }
    public string SkuId { get; private set; } = null!;
    public decimal BasePrice { get; private set; }
    public IReadOnlyList<TierPrice> TierPrices { get; private set; } = Array.Empty<TierPrice>();

    private PriceListItem() { }

    public static PriceListItem Create(Guid priceListId, string skuId, decimal basePrice, IReadOnlyList<TierPrice>? tierPrices)
    {
        if (string.IsNullOrWhiteSpace(skuId)) throw new ArgumentException("SkuId required.");

        return new PriceListItem
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            SkuId = skuId.Trim(),
            BasePrice = basePrice,
            TierPrices = tierPrices ?? Array.Empty<TierPrice>()
        };
    }

    public void Update(decimal basePrice, IReadOnlyList<TierPrice>? tierPrices)
    {
        BasePrice = basePrice;
        TierPrices = tierPrices ?? Array.Empty<TierPrice>();
        Touch();
    }
}
