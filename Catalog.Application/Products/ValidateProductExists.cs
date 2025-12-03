using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

/// <summary>
/// Query to validate if a product exists and optionally if a variant belongs to that product.
/// Used by Inventory service for product validation.
/// </summary>
public sealed record ValidateProductExistsQuery(
    Guid ProductId,
    Guid? VariantId = null
) : IRequest<bool>;

public sealed class ValidateProductExistsHandler : IRequestHandler<ValidateProductExistsQuery, bool>
{
    private readonly DbContext _db;

    public ValidateProductExistsHandler(DbContext db) => _db = db;

    public async Task<bool> Handle(ValidateProductExistsQuery req, CancellationToken ct)
    {
        // Check if product exists
        var productExists = await _db.Set<Product>()
            .AnyAsync(p => p.Id == req.ProductId, ct);

        if (!productExists) return false;

        // If VariantId is provided, check if it belongs to the product
        if (req.VariantId.HasValue)
        {
            var variantExists = await _db.Set<ProductVariant>()
                .AnyAsync(v => v.Id == req.VariantId.Value 
                               && v.ProductId == req.ProductId 
                               && v.IsActive, ct);
            return variantExists;
        }

        return true;
    }
}

