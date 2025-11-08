using Catalog.Domain.Products;
using Catalog.Domain.Stores;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class UpsertProductStoreHandler : IRequestHandler<UpsertProductStoreCommand, Unit>
{
    private readonly DbContext _db;
    public UpsertProductStoreHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(UpsertProductStoreCommand req, CancellationToken ct)
    {
        // وجود Product و Store
        if (!await _db.Set<Product>().AnyAsync(p => p.Id == req.ProductId, ct))
            throw new InvalidOperationException("Product not found.");
        if (!await _db.Set<Store>().AnyAsync(s => s.Id == req.StoreId, ct))
            throw new InvalidOperationException("Store not found.");

        // آیا لینک موجود است؟
        var link = await _db.Set<ProductStore>()
            .FirstOrDefaultAsync(x => x.ProductId == req.ProductId && x.StoreId == req.StoreId, ct);

        if (link is null)
        {
            // INSERT صریح
            link = ProductStore.Create(
                productId: req.ProductId,
                storeId: req.StoreId,
                isVisible: req.IsVisible,
                slug: req.Slug,
                title: req.TitleOverride,
                description: req.DescriptionOverride
            );
            _db.Set<ProductStore>().Add(link);
        }
        else
        {
            // UPDATE
            link.Update(req.IsVisible, req.Slug, req.TitleOverride, req.DescriptionOverride);
        }

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}