using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Gateways;

namespace Inventory.Api;

/// <summary>
/// Adapter to bridge CatalogApiGateway (Infrastructure) with ICatalogGateway (Application interface)
/// This avoids circular dependency between Infrastructure and Application projects.
/// </summary>
internal sealed class CatalogGatewayAdapter : ICatalogGateway
{
    private readonly CatalogApiGateway _gateway;

    public CatalogGatewayAdapter(CatalogApiGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<CatalogItemDto?> GetCatalogItemAsync(Guid productId, Guid? variantId = null, CancellationToken ct = default)
    {
        var result = await _gateway.GetCatalogItemAsync(productId, variantId, ct);
        if (result is null) return null;

        return new CatalogItemDto(result.Sku, result.Name, result.IsVariant);
    }
}

