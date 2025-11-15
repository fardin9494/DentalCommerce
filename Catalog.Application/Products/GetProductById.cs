using Catalog.Domain.Brands;
using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDetailDto?>;

public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    private readonly DbContext _db;
    public GetProductByIdHandler(DbContext db) => _db = db;

    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery req, CancellationToken ct)
    {
        var product = await _db.Set<Product>()
            .AsNoTracking()
            .Where(p => p.Id == req.ProductId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                p.WarehouseCode,
                p.DefaultSlug,
                p.BrandId,
                BrandName = _db.Set<Brand>()
                    .Where(b => b.Id == p.BrandId)
                    .Select(b => b.Name)
                    .FirstOrDefault(),
                p.CountryCode,
                p.Status,
                p.PrimaryCategoryId,
                p.Description,
                p.VariationKey,
                p.MainImageId
            })
            .FirstOrDefaultAsync(ct);

        if (product is null) return null;

        var variants = await _db.Set<ProductVariant>()
            .AsNoTracking()
            .Where(v => v.ProductId == req.ProductId)
            .OrderBy(v => v.Value)
            .Select(v => new VariantDto
            {
                Id = v.Id,
                Value = v.Value,
                Sku = v.Sku,
                IsActive = v.IsActive
            })
            .ToListAsync(ct);

        var properties = await _db.Set<ProductProperty>()
            .AsNoTracking()
            .Where(pp => pp.ProductId == req.ProductId)
            .OrderBy(pp => pp.Key)
            .Select(pp => new PropertyDto
            {
                Id = pp.Id,
                Key = pp.Key,
                ValueString = pp.ValueString,
                ValueDecimal = pp.ValueDecimal,
                ValueBool = pp.ValueBool,
                ValueJson = pp.ValueJson
            })
            .ToListAsync(ct);

        var images = await _db.Set<ProductImage>()
            .AsNoTracking()
            .Where(i => i.ProductId == req.ProductId)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ImageDto
            {
                Id = i.Id,
                Url = i.Url,
                Alt = i.Alt,
                SortOrder = i.SortOrder,
                IsMain = product.MainImageId != null && product.MainImageId == i.Id
            })
            .ToListAsync(ct);

        var categories = await _db.Set<ProductCategory>()
            .AsNoTracking()
            .Where(c => c.ProductId == req.ProductId)
            .Select(c => new CategoryLinkDto
            {
                CategoryId = c.CategoryId,
                IsPrimary = c.IsPrimary
            })
            .ToListAsync(ct);

        var stores = await _db.Set<ProductStore>()
            .AsNoTracking()
            .Where(s => s.ProductId == req.ProductId)
            .Select(s => new ProductStoreDto
            {
                StoreId = s.StoreId,
                IsVisible = s.IsVisible,
                Slug = s.Slug,
                TitleOverride = s.TitleOverride,
                DescriptionOverride = s.DescriptionOverride
            })
            .ToListAsync(ct);

        var seos = await _db.Set<ProductSeo>()
            .AsNoTracking()
            .Where(s => s.ProductId == req.ProductId)
            .Select(s => new ProductSeoDto
            {
                StoreId = s.StoreId,
                MetaTitle = s.MetaTitle,
                MetaDescription = s.MetaDescription,
                CanonicalUrl = s.CanonicalUrl,
                Robots = s.Robots,
                JsonLd = s.JsonLd
            })
            .ToListAsync(ct);

        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Code = product.Code,
            WarehouseCode = product.WarehouseCode,
            DefaultSlug = product.DefaultSlug,
            BrandId = product.BrandId,
            BrandName = product.BrandName ?? string.Empty,
            CountryCode = product.CountryCode,
            Status = product.Status.ToString(),
            PrimaryCategoryId = product.PrimaryCategoryId,
            Description = product.Description ?? string.Empty,
            VariationKey = product.VariationKey,
            Variants = variants,
            Properties = properties,
            Images = images,
            Categories = categories,
            Stores = stores,
            Seos = seos
        };
    }
}
