using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class HideProductHandler : IRequestHandler<HideProductCommand, Unit>
{
    private readonly DbContext _db;
    public HideProductHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(HideProductCommand req, CancellationToken ct)
    {
        var product = await _db.Set<Product>()
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

        if (product is null)
            throw new InvalidOperationException("Product not found.");

        product.SetStatus(ProductStatus.Hidden);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

