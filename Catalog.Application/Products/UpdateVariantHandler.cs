using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class UpdateVariantHandler : IRequestHandler<UpdateVariantCommand, Unit>
{
    private readonly DbContext _db;
    public UpdateVariantHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateVariantCommand req, CancellationToken ct)
    {
        var product = await _db.Set<Product>()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

        if (product is null)
            throw new InvalidOperationException("Product not found.");

        if (product.VariationMode != VariationMode.SingleAttribute)
            throw new InvalidOperationException("Product variation mode is not SingleAttribute.");

        if (string.IsNullOrWhiteSpace(product.VariationKey))
            throw new InvalidOperationException("Product variation key is not set.");

        var v = product.Variants.FirstOrDefault(x => x.Id == req.VariantId);
        if (v is null)
            throw new InvalidOperationException("Variant not found.");

        var newValue = req.VariantValue?.Trim() ?? string.Empty;
        var newSku = req.Sku?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newValue))
            throw new InvalidOperationException("Variant value is required.");

        // Ensure uniqueness of value within the product
        var duplicate = product.Variants.Any(x => x.Id != v.Id && x.Value == newValue);
        if (duplicate)
            throw new InvalidOperationException("Duplicate variant value for this product.");

        // Apply changes
        v.UpdateValue(newValue); // assuming domain exposes method; fallback to property if needed
        v.UpdateSku(newSku);
        v.SetActive(req.IsActive);

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

