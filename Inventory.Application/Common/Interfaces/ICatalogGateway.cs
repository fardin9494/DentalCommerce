namespace Inventory.Application.Common.Interfaces;

/// <summary>
/// Anti-Corruption Layer (ACL) interface for fetching product data from Catalog service.
/// This abstraction ensures Inventory Bounded Context remains decoupled from Catalog Domain.
/// </summary>
public interface ICatalogGateway
{
    /// <summary>
    /// Fetches catalog item information (SKU and name) for a given product and optional variant.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="variantId">Optional variant identifier. If provided, returns variant SKU; otherwise returns product code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Catalog item DTO with SKU, name, and variant flag. Returns null if product not found.</returns>
    Task<CatalogItemDto?> GetCatalogItemAsync(Guid productId, Guid? variantId = null, CancellationToken ct = default);
}

/// <summary>
/// Data Transfer Object for catalog item information.
/// </summary>
/// <param name="Sku">The SKU (Stock Keeping Unit) - either variant SKU or product code.</param>
/// <param name="Name">The product or variant name.</param>
/// <param name="IsVariant">Indicates whether this is a variant (true) or base product (false).</param>
public record CatalogItemDto(string Sku, string Name, bool IsVariant);

