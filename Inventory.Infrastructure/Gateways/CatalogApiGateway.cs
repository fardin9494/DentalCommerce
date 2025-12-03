using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Gateways;

/// <summary>
/// Anti-Corruption Layer (ACL) implementation for Catalog API.
/// Fetches product data from Catalog service via HTTP API.
/// </summary>
/// <remarks>
/// This class implements the interface defined in Inventory.Application.Common.Interfaces
/// but does not reference that project to avoid circular dependencies.
/// The interface contract is maintained through runtime dependency injection via adapter.
/// </remarks>
public sealed class CatalogApiGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogApiGateway> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CatalogApiGateway(IHttpClientFactory httpClientFactory, ILogger<CatalogApiGateway> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CatalogApi");
        _logger = logger;
    }

    public async Task<CatalogItemDto?> GetCatalogItemAsync(Guid productId, Guid? variantId = null, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/products/{productId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product {ProductId} not found in Catalog service", productId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var product = JsonSerializer.Deserialize<ProductDetailResponse>(json, JsonOptions);

            if (product is null)
            {
                _logger.LogWarning("Failed to deserialize product {ProductId} response", productId);
                return null;
            }

            // SKU Resolution Logic:
            // 1. If VariantId is provided, look for the specific variant in the product's variant list
            // 2. If VariantId is null, use the parent Product.Code

            if (variantId.HasValue)
            {
                var variant = product.Variants?.FirstOrDefault(v => v.Id == variantId.Value);
                
                // Strict validation: If variantId is provided, variant MUST exist and be active
                if (variant is null)
                {
                    _logger.LogWarning(
                        "Variant {VariantId} not found for product {ProductId}",
                        variantId.Value,
                        productId
                    );
                    return null; // Return null to fail validation
                }

                // Check if variant is active
                if (!variant.IsActive)
                {
                    _logger.LogWarning(
                        "Variant {VariantId} for product {ProductId} is not active",
                        variantId.Value,
                        productId
                    );
                    return null; // Return null to fail validation
                }

                return new CatalogItemDto(
                    Sku: variant.Sku ?? product.Code, // Fallback to product code if variant SKU is empty
                    Name: $"{product.Name} - {variant.Value}",
                    IsVariant: true
                );
            }

            // Use product code when no variant specified
            return new CatalogItemDto(
                Sku: product.Code ?? throw new InvalidOperationException($"Product {productId} has no Code"),
                Name: product.Name,
                IsVariant: false
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Catalog API for product {ProductId}", productId);
            throw new InvalidOperationException($"Failed to fetch product {productId} from Catalog service", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout calling Catalog API for product {ProductId}", productId);
            throw new InvalidOperationException($"Timeout while fetching product {productId} from Catalog service", ex);
        }
    }

    // Internal DTOs for deserializing Catalog API response
    // These are kept private to maintain ACL boundaries
    private sealed class ProductDetailResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public string Code { get; init; } = default!;
        public IReadOnlyList<VariantResponse>? Variants { get; init; }
    }

    private sealed class VariantResponse
    {
        public Guid Id { get; init; }
        public string Value { get; init; } = default!;
        public string? Sku { get; init; }
        public bool IsActive { get; init; }
    }

    // DTO matching the interface contract (duplicated to avoid circular dependency)
    public sealed record CatalogItemDto(string Sku, string Name, bool IsVariant);
}
