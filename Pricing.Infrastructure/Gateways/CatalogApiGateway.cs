using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Pricing.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Pricing.Infrastructure.Gateways;

public sealed class CatalogApiGateway : ICatalogPricingGateway
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);
    private const int CacheMaxEntries = 1000;
    private const int MaxConcurrency = 8;
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim FetchSemaphore = new(MaxConcurrency, MaxConcurrency);

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

    public async Task<IReadOnlyDictionary<string, CatalogSkuInfo>> GetSkuInfosAsync(IReadOnlyCollection<string> skuIds, CancellationToken ct)
    {
        var result = new Dictionary<string, CatalogSkuInfo>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        PruneCache(now);

        var missing = new List<string>();

        foreach (var sku in skuIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Cache.TryGetValue(sku, out var entry) && entry.ExpiresAt > now)
            {
                result[sku] = entry.Value;
            }
            else
            {
                Cache.TryRemove(sku, out _);
                missing.Add(sku);
            }
        }

        if (missing.Count == 0) return result;

        var tasks = missing.Select(sku => ResolveWithThrottleAsync(sku, ct));

        (string Sku, CatalogSkuInfo? Info)[] resolved;
        try
        {
            resolved = await Task.WhenAll(tasks);
        }
        catch (CatalogAccessDeniedException ex)
        {
            throw new InvalidOperationException(
                "Access to Catalog metadata was denied. Required permissions: Catalog.Products.ResolveBySku and Catalog.Products.View.",
                ex);
        }

        foreach (var item in resolved)
        {
            if (item.Info is null) continue;
            result[item.Sku] = item.Info;
            Cache[item.Sku] = new CacheEntry(item.Info, now.Add(CacheTtl));
        }

        return result;
    }

    private static void PruneCache(DateTime now)
    {
        foreach (var entry in Cache)
        {
            if (entry.Value.ExpiresAt <= now)
                Cache.TryRemove(entry.Key, out _);
        }

        if (Cache.Count <= CacheMaxEntries) return;

        var overflow = Cache.Count - CacheMaxEntries;
        foreach (var entry in Cache.OrderBy(x => x.Value.ExpiresAt).Take(overflow))
        {
            Cache.TryRemove(entry.Key, out _);
        }
    }

    private async Task<(string Sku, CatalogSkuInfo? Info)> ResolveWithThrottleAsync(string sku, CancellationToken ct)
    {
        await FetchSemaphore.WaitAsync(ct);
        try
        {
            var info = await TryResolveBySkuAsync(sku, ct);
            info ??= await TryResolveBySearchAsync(sku, ct);
            return (Sku: sku, Info: info);
        }
        finally
        {
            FetchSemaphore.Release();
        }
    }

    private async Task<CatalogSkuInfo?> TryResolveBySkuAsync(string sku, CancellationToken ct)
    {
        try
        {
            var url = $"/api/catalog/products/by-sku?sku={Uri.EscapeDataString(sku)}";
            var response = await _httpClient.GetAsync(url, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new CatalogAccessDeniedException(response.StatusCode);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var resolved = JsonSerializer.Deserialize<SkuResolutionResponse>(json, JsonOptions);
            if (resolved is null)
                return null;

            return await LoadProductDetailsAsync(resolved.ProductId, sku, ct);
        }
        catch (CatalogAccessDeniedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve SKU {SkuId} via by-sku endpoint", sku);
            return null;
        }
    }

    private async Task<CatalogSkuInfo?> TryResolveBySearchAsync(string sku, CancellationToken ct)
    {
        try
        {
            var url = $"/api/catalog/products?search={Uri.EscapeDataString(sku)}&page=1&pageSize=10";
            var response = await _httpClient.GetAsync(url, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new CatalogAccessDeniedException(response.StatusCode);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var list = JsonSerializer.Deserialize<ProductListResponse>(json, JsonOptions);
            var items = list?.Items ?? new List<ProductListItem>();
            if (items.Count == 0)
                return null;

            var codeMatch = items.FirstOrDefault(p => string.Equals(p.Code, sku, StringComparison.OrdinalIgnoreCase));
            if (codeMatch is not null)
            {
                var byCode = await LoadProductDetailsAsync(codeMatch.Id, sku, ct);
                if (byCode is not null) return byCode;
            }

            foreach (var item in items)
            {
                var info = await LoadProductDetailsAsync(item.Id, sku, ct);
                if (info is not null) return info;
            }

            return null;
        }
        catch (CatalogAccessDeniedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve SKU {SkuId} from Catalog API", sku);
            return null;
        }
    }

    private async Task<CatalogSkuInfo?> LoadProductDetailsAsync(Guid productId, string sku, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/products/{productId}", ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new CatalogAccessDeniedException(response.StatusCode);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var product = JsonSerializer.Deserialize<ProductDetailResponse>(json, JsonOptions);
            if (product is null)
                return null;

            var tags = ExtractTags(product.Properties);
            var isBatchSelectable = ExtractBool(product.Properties, "isBatchSelectable", "batchSelectable");
            var minAllowedPrice = ExtractDecimal(product.Properties, "minAllowedPrice", "priceFloor", "minPrice");

            var variant = product.Variants
                .FirstOrDefault(v => v.IsActive
                                     && !string.IsNullOrWhiteSpace(v.Sku)
                                     && string.Equals(v.Sku, sku, StringComparison.OrdinalIgnoreCase));

            var variantId = variant?.Id;
            var isProductCode = string.Equals(product.Code, sku, StringComparison.OrdinalIgnoreCase);
            if (!isProductCode && variantId is null)
            {
                _logger.LogWarning("SKU {SkuId} did not match product code or variant in Catalog product {ProductId}", sku, productId);
                return null;
            }

            return new CatalogSkuInfo(
                SkuId: sku,
                ProductId: product.Id,
                VariantId: variantId,
                BrandId: product.BrandId,
                CategoryIds: product.Categories.Select(c => c.CategoryId).ToList(),
                Tags: tags,
                IsBatchSelectable: isBatchSelectable,
                MinAllowedPrice: minAllowedPrice);
        }
        catch (CatalogAccessDeniedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Catalog details for product {ProductId}", productId);
            return null;
        }
    }

    private static IReadOnlyList<string> ExtractTags(IReadOnlyList<PropertyDto> props)
    {
        var prop = props.FirstOrDefault(p => string.Equals(p.Key, "tags", StringComparison.OrdinalIgnoreCase));
        if (prop is null) return Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(prop.ValueJson))
        {
            try
            {
                var tags = JsonSerializer.Deserialize<List<string>>(prop.ValueJson, JsonOptions);
                if (tags is not null) return tags;
            }
            catch
            {
                // Ignore malformed tag json.
            }
        }

        if (!string.IsNullOrWhiteSpace(prop.ValueString))
            return prop.ValueString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Array.Empty<string>();
    }

    private static bool ExtractBool(IReadOnlyList<PropertyDto> props, params string[] keys)
    {
        foreach (var key in keys)
        {
            var prop = props.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
            if (prop?.ValueBool is not null)
                return prop.ValueBool.Value;
        }

        return false;
    }

    private static decimal? ExtractDecimal(IReadOnlyList<PropertyDto> props, params string[] keys)
    {
        foreach (var key in keys)
        {
            var prop = props.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
            if (prop?.ValueDecimal is not null)
                return prop.ValueDecimal.Value;
        }

        return null;
    }

    private sealed record CacheEntry(CatalogSkuInfo Value, DateTime ExpiresAt);

    private sealed class CatalogAccessDeniedException : Exception
    {
        public CatalogAccessDeniedException(HttpStatusCode statusCode)
            : base($"Catalog API denied access with status {(int)statusCode}.")
        {
        }
    }

    private sealed class ProductListResponse
    {
        public List<ProductListItem>? Items { get; init; }
    }

    private sealed class ProductListItem
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
    }

    private sealed class SkuResolutionResponse
    {
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
    }

    private sealed class ProductDetailResponse
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public Guid BrandId { get; init; }
        public IReadOnlyList<CategoryLinkDto> Categories { get; init; } = Array.Empty<CategoryLinkDto>();
        public IReadOnlyList<PropertyDto> Properties { get; init; } = Array.Empty<PropertyDto>();
        public IReadOnlyList<VariantDto> Variants { get; init; } = Array.Empty<VariantDto>();
    }

    private sealed class VariantDto
    {
        public Guid Id { get; init; }
        public string? Sku { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class CategoryLinkDto
    {
        public Guid CategoryId { get; init; }
    }

    private sealed class PropertyDto
    {
        public string Key { get; init; } = string.Empty;
        public string? ValueString { get; init; }
        public decimal? ValueDecimal { get; init; }
        public bool? ValueBool { get; init; }
        public string? ValueJson { get; init; }
    }
}
