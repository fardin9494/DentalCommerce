namespace Pricing.Application.Abstractions;

public interface ICatalogPricingGateway
{
    Task<IReadOnlyDictionary<string, CatalogSkuInfo>> GetSkuInfosAsync(
        IReadOnlyCollection<string> skuIds,
        CancellationToken ct);
}

public sealed record CatalogSkuInfo(
    string SkuId,
    Guid? ProductId,
    Guid? VariantId,
    Guid? BrandId,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<string> Tags,
    bool IsBatchSelectable,
    decimal? MinAllowedPrice);
