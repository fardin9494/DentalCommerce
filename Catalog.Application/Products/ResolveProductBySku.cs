using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

/// <summary>
/// Resolve a product (and optional variant) by exact SKU match.
/// Used by Pricing to map SKU to catalog metadata.
/// </summary>
public sealed record ResolveProductBySkuQuery(string Sku) : IRequest<SkuResolutionDto?>;

public sealed record SkuResolutionDto(Guid ProductId, Guid? VariantId);

public sealed class ResolveProductBySkuHandler : IRequestHandler<ResolveProductBySkuQuery, SkuResolutionDto?>
{
    private readonly DbContext _db;

    public ResolveProductBySkuHandler(DbContext db) => _db = db;

    public async Task<SkuResolutionDto?> Handle(ResolveProductBySkuQuery req, CancellationToken ct)
    {
        var sku = req.Sku?.Trim();
        if (string.IsNullOrWhiteSpace(sku)) return null;

        var productMatch = await _db.Set<Product>()
            .AsNoTracking()
            .Where(p => p.Code == sku)
            .Select(p => new SkuResolutionDto(p.Id, null))
            .FirstOrDefaultAsync(ct);

        if (productMatch is not null) return productMatch;

        var variantMatch = await _db.Set<ProductVariant>()
            .AsNoTracking()
            .Where(v => v.Sku == sku)
            .Select(v => new SkuResolutionDto(v.ProductId, v.Id))
            .FirstOrDefaultAsync(ct);

        return variantMatch;
    }
}
