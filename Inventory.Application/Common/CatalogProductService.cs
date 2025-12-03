using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Common;

/// <summary>
/// Anti-Corruption Layer برای ارتباط با Catalog Context
/// این سرویس مسئولیت validation و lookup محصولات از Catalog را دارد
/// </summary>
public interface ICatalogProductService
{
    /// <summary>
    /// بررسی می‌کند که ProductId معتبر است و در Catalog وجود دارد
    /// </summary>
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// بررسی می‌کند که VariantId متعلق به ProductId است
    /// </summary>
    Task<bool> VariantExistsAsync(Guid productId, Guid variantId, CancellationToken ct = default);

    /// <summary>
    /// دریافت اطلاعات پایه محصول (برای نمایش در گزارشات)
    /// </summary>
    Task<ProductInfoDto?> GetProductInfoAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// دریافت SKU برای محصول یا variant
    /// اگر variantId null باشد، SKU از Product.Code گرفته می‌شود
    /// اگر variantId داشته باشد، SKU از ProductVariant.Sku گرفته می‌شود
    /// </summary>
    Task<string> GetSkuAsync(Guid productId, Guid? variantId, CancellationToken ct = default);
}

/// <summary>
/// DTO برای اطلاعات پایه محصول از Catalog
/// </summary>
public sealed record ProductInfoDto(
    Guid Id,
    string Name,
    string Code,
    string? WarehouseCode,
    bool IsActive
);

// ==========================================
// پیاده‌سازی 1: HTTP API (برای Microservices)
// ==========================================

/// <summary>
/// پیاده‌سازی سرویس با استفاده از HTTP Client برای فراخوانی Catalog API
/// استفاده کنید اگر Catalog و Inventory در سرویس‌های جداگانه اجرا می‌شوند
/// </summary>
public sealed class CatalogProductHttpService : ICatalogProductService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogProductHttpService> _logger;

    public CatalogProductHttpService(HttpClient httpClient, ILogger<CatalogProductHttpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/products/{productId}/exists", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "خطا در بررسی وجود محصول {ProductId}", productId);
            throw new InvalidOperationException($"خطا در ارتباط با Catalog: {ex.Message}", ex);
        }
    }

    public async Task<bool> VariantExistsAsync(Guid productId, Guid variantId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/products/{productId}/variants/{variantId}/exists", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "خطا در بررسی وجود variant {VariantId} برای محصول {ProductId}", variantId, productId);
            throw new InvalidOperationException($"خطا در ارتباط با Catalog: {ex.Message}", ex);
        }
    }

    public async Task<ProductInfoDto?> GetProductInfoAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/products/{productId}", ct);
            if (!response.IsSuccessStatusCode) return null;

            var product = await response.Content.ReadFromJsonAsync<ProductInfoDto>(ct);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "خطا در دریافت اطلاعات محصول {ProductId}", productId);
            return null;
        }
    }

    public async Task<string> GetSkuAsync(Guid productId, Guid? variantId, CancellationToken ct = default)
    {
        try
        {
            if (variantId.HasValue)
            {
                // دریافت SKU از Variant
                var response = await _httpClient.GetAsync($"/api/catalog/products/{productId}/variants/{variantId.Value}/sku", ct);
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Variant {variantId.Value} برای محصول {productId} یافت نشد.");
                
                var sku = await response.Content.ReadAsStringAsync(ct);
                return sku.Trim('"'); // Remove quotes if JSON string
            }
            else
            {
                // دریافت SKU از Product Code
                var productInfo = await GetProductInfoAsync(productId, ct);
                if (productInfo is null)
                    throw new InvalidOperationException($"محصول {productId} یافت نشد.");
                
                return productInfo.Code;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت SKU برای محصول {ProductId}, Variant {VariantId}", productId, variantId);
            throw new InvalidOperationException($"خطا در دریافت SKU: {ex.Message}", ex);
        }
    }
}

// ==========================================
// پیاده‌سازی 2: Direct Database Access (برای Monolith)
// ==========================================

/// <summary>
/// پیاده‌سازی سرویس با دسترسی مستقیم به Catalog DbContext
/// استفاده کنید اگر Catalog و Inventory در یک Monolith هستند و از یک دیتابیس استفاده می‌کنند
/// 
/// نکته: برای استفاده از این پیاده‌سازی، باید Catalog.Infrastructure را به Inventory.Application اضافه کنید
/// </summary>
public sealed class CatalogProductDbService : ICatalogProductService
{
    // این پیاده‌سازی نیاز به reference به Catalog.Infrastructure دارد
    // برای استفاده، در Inventory.Application.csproj اضافه کنید:
    // <ProjectReference Include="..\Catalog.Infrastructure\Catalog.Infrastructure.csproj" />
    
    // Uncomment the following code after adding the reference:
    /*
    private readonly Catalog.Infrastructure.CatalogDbContext _catalogDb;
    
    public CatalogProductDbService(Catalog.Infrastructure.CatalogDbContext catalogDb)
    {
        _catalogDb = catalogDb;
    }
    
    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default)
    {
        return await _catalogDb.Set<Catalog.Domain.Products.Product>()
            .AnyAsync(p => p.Id == productId, ct);
    }
    
    public async Task<bool> VariantExistsAsync(Guid productId, Guid variantId, CancellationToken ct = default)
    {
        return await _catalogDb.Set<Catalog.Domain.Products.ProductVariant>()
            .AnyAsync(v => v.Id == variantId && v.ProductId == productId && v.IsActive, ct);
    }
    
    public async Task<ProductInfoDto?> GetProductInfoAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _catalogDb.Set<Catalog.Domain.Products.Product>()
            .Where(p => p.Id == productId)
            .Select(p => new ProductInfoDto(
                p.Id,
                p.Name,
                p.Code,
                p.WarehouseCode,
                p.Status == Catalog.Domain.Products.ProductStatus.Active
            ))
            .FirstOrDefaultAsync(ct);
        
        return product;
    }
    
    public async Task<string> GetSkuAsync(Guid productId, Guid? variantId, CancellationToken ct = default)
    {
        if (variantId.HasValue)
        {
            // دریافت SKU از Variant
            var variant = await _catalogDb.Set<Catalog.Domain.Products.ProductVariant>()
                .Where(v => v.Id == variantId.Value && v.ProductId == productId && v.IsActive)
                .Select(v => v.Sku)
                .FirstOrDefaultAsync(ct);
            
            if (string.IsNullOrWhiteSpace(variant))
                throw new InvalidOperationException($"Variant {variantId.Value} برای محصول {productId} یافت نشد.");
            
            return variant;
        }
        else
        {
            // دریافت SKU از Product Code
            var productCode = await _catalogDb.Set<Catalog.Domain.Products.Product>()
                .Where(p => p.Id == productId)
                .Select(p => p.Code)
                .FirstOrDefaultAsync(ct);
            
            if (string.IsNullOrWhiteSpace(productCode))
                throw new InvalidOperationException($"محصول {productId} یافت نشد.");
            
            return productCode;
        }
    }
    */

    // Temporary implementation to prevent compile errors
    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default)
    {
        throw new NotImplementedException("برای استفاده از این پیاده‌سازی، Catalog.Infrastructure را به Inventory.Application اضافه کنید");
    }

    public Task<bool> VariantExistsAsync(Guid productId, Guid variantId, CancellationToken ct = default)
    {
        throw new NotImplementedException("برای استفاده از این پیاده‌سازی، Catalog.Infrastructure را به Inventory.Application اضافه کنید");
    }

    public Task<ProductInfoDto?> GetProductInfoAsync(Guid productId, CancellationToken ct = default)
    {
        throw new NotImplementedException("برای استفاده از این پیاده‌سازی، Catalog.Infrastructure را به Inventory.Application اضافه کنید");
    }

    public Task<string> GetSkuAsync(Guid productId, Guid? variantId, CancellationToken ct = default)
    {
        throw new NotImplementedException("برای استفاده از این پیاده‌سازی، Catalog.Infrastructure را به Inventory.Application اضافه کنید");
    }
}
