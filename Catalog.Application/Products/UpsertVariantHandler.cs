using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class UpsertVariantHandler : IRequestHandler<UpsertVariantCommand, Guid>
{
    private readonly DbContext _db;
    public UpsertVariantHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertVariantCommand req, CancellationToken ct)
    {
        var product = await _db.Set<Product>()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

        if (product is null)
            throw new InvalidOperationException("Product not found.");

        // باید محصول در حالت SingleAttribute باشد
        if (product.VariationMode != VariationMode.SingleAttribute)
            throw new InvalidOperationException("Product variation mode is not SingleAttribute.");

        if (string.IsNullOrWhiteSpace(product.VariationKey))
            throw new InvalidOperationException("Product variation key is not set.");

        // افزودن/به‌روزرسانی
        var v = product.Variants.FirstOrDefault(x => x.Value == req.VariantValue.Trim());
        if (v is null)
        {
            v = ProductVariant.Create(product.Id, req.VariantValue.Trim(), req.Sku.Trim(), req.IsActive);
            // حتماً به EF معرفی شود:
            _db.Set<ProductVariant>().Add(v);
        }
        else
        {
            v.UpdateSku(req.Sku);
            v.SetActive(req.IsActive);
        }

        await _db.SaveChangesAsync(ct);
        return v.Id;
    }
}