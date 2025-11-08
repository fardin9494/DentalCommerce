using Catalog.Domain.Products;
using Catalog.Domain.Stores;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class UpsertProductSeoHandler : IRequestHandler<UpsertProductSeoCommand, Unit>
{
    private readonly DbContext _db;
    public UpsertProductSeoHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(UpsertProductSeoCommand req, CancellationToken ct)
    {
        // وجود محصول و استور
        if (!await _db.Set<Product>().AnyAsync(p => p.Id == req.ProductId, ct))
            throw new InvalidOperationException("Product not found.");
        if (!await _db.Set<Store>().AnyAsync(s => s.Id == req.StoreId, ct))
            throw new InvalidOperationException("Store not found.");

        // آیا رکورد SEO برای این (Product, Store) هست؟
        var seo = await _db.Set<ProductSeo>()
            .FirstOrDefaultAsync(x => x.ProductId == req.ProductId && x.StoreId == req.StoreId, ct);

        if (seo is null)
        {
            // INSERT صریح
            seo = ProductSeo.Create(req.ProductId, req.StoreId,
                req.MetaTitle, req.MetaDescription,
                req.CanonicalUrl, req.Robots, req.JsonLd);

            _db.Set<ProductSeo>().Add(seo);            // 👈 خیلی مهم
        }
        else
        {
            // UPDATE
            seo.Update(req.MetaTitle, req.MetaDescription,
                req.CanonicalUrl, req.Robots, req.JsonLd);
        }

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}