namespace Pricing.Application.Features.PriceLists;

public sealed class PriceListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<PriceListItemDto> Items { get; init; } = Array.Empty<PriceListItemDto>();
}

public sealed class PriceListItemDto
{
    public Guid Id { get; init; }
    public string SkuId { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public IReadOnlyList<TierPriceDto> TierPrices { get; init; } = Array.Empty<TierPriceDto>();
}

public sealed class TierPriceDto
{
    public int MinQty { get; init; }
    public decimal UnitPrice { get; init; }
}
