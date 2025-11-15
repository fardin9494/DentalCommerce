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
        var q = _db.Set<Product>().AsNoTracking()
            .Where(p => p.Id == req.ProductId)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                WarehouseCode = p.WarehouseCode,
                DefaultSlug = p.DefaultSlug,
                BrandId = p.BrandId,
                BrandName = _db.Set<Brand>().Where(b => b.Id == p.BrandId).Select(b => b.Name).First(),
                CountryCode = p.CountryCode,
                Status = p.Status.ToString(),
                PrimaryCategoryId = p.PrimaryCategoryId,
                Description = p.Description,
				VariationKey = p.VariationKey,

                Variants = p.Variants
                    .OrderBy(v => v.Value)
                    .Select(v => new VariantDto { Id = v.Id, Value = v.Value, Sku = v.Sku, IsActive = v.IsActive })
                    .ToList(),

                Properties = p.Properties
                    .OrderBy(pp => pp.Key)
                    .Select(pp => new PropertyDto
                    {
                        Id = pp.Id,
                        Key = pp.Key,
                        ValueString = pp.ValueString,
                        ValueDecimal = pp.ValueDecimal,
                        ValueBool = pp.ValueBool,
                        ValueJson = pp.ValueJson
                    }).ToList(),

                Images = p.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ImageDto
                    {
                        Id = i.Id,
                        Url = i.Url,
                        Alt = i.Alt,
                        SortOrder = i.SortOrder,
                        IsMain = p.MainImageId != null && p.MainImageId == i.Id
                    }).ToList(),

                Categories = p.Categories
                    .Select(c => new CategoryLinkDto { CategoryId = c.CategoryId, IsPrimary = c.IsPrimary })
                    .ToList(),

                Stores = p.Stores
                    .Select(s => new ProductStoreDto
                    {
                        StoreId = s.StoreId,
                        IsVisible = s.IsVisible,
                        Slug = s.Slug,
                        TitleOverride = s.TitleOverride,
                        DescriptionOverride = s.DescriptionOverride
                    }).ToList(),

                Seos = p.Seos
                    .Select(s => new ProductSeoDto
                    {
                        StoreId = s.StoreId,
                        MetaTitle = s.MetaTitle,
                        MetaDescription = s.MetaDescription,
                        CanonicalUrl = s.CanonicalUrl,
                        Robots = s.Robots,
                        JsonLd = s.JsonLd
                    }).ToList(),
            });

        return await q.FirstOrDefaultAsync(ct);
    }
}
