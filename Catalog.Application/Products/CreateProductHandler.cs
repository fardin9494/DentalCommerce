using Catalog.Domain.Products;
using Catalog.Domain.Brands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly DbContext _db; // CatalogDbContext تزریق می‌شود

    public CreateProductHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(CreateProductCommand req, CancellationToken ct)
    {
        // برند باید وجود داشته باشد
        var brandExists = await _db.Set<Brand>().AnyAsync(b => b.Id == req.BrandId, ct);
        if (!brandExists) throw new InvalidOperationException("Brand not found.");

        // یکتایی Code
        var code = req.Code.Trim().ToUpperInvariant();
        var dup = await _db.Set<Product>().AnyAsync(p => p.Code == code, ct);
        if (dup) throw new InvalidOperationException("Duplicate product code.");

        var product = Product.Create(
            name: req.Name,
            defaultSlug: req.Slug,
            code: code,
            brandId: req.BrandId,
            warehouseCode: req.WarehouseCode,
            countryCode: req.CountryCode
        );

        if (!string.IsNullOrWhiteSpace(req.CountryCode))
        {
            var cc = req.CountryCode!.Trim().ToUpperInvariant();
            var exists = await _db.Set<Catalog.Domain.Brands.Country>().AnyAsync(c => c.Code2 == cc, ct);
            if (!exists) throw new InvalidOperationException("Country not found.");
        }

        if (!string.IsNullOrWhiteSpace(req.VariationKey))
            product.SetVariation(req.VariationKey);

        bool first = true;
        foreach (var catId in req.CategoryIds.Distinct())
        {
            product.AddCategory(catId, makePrimary: first && product.PrimaryCategoryId is null);
            first = false;
        }

        _db.Set<Product>().Add(product);
        await _db.SaveChangesAsync(ct);
        return product.Id;
    }
}
