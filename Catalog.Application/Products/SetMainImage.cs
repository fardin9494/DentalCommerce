using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record SetMainImageCommand(Guid ProductId, Guid ImageId) : IRequest<Unit>;

public sealed class SetMainImageHandler : IRequestHandler <SetMainImageCommand, Unit>
{
private readonly DbContext _db;
public SetMainImageHandler(DbContext db) => _db = db;

public async Task<Unit> Handle(SetMainImageCommand req, CancellationToken ct)
{
    var exists = await _db.Set<ProductImage>().AsNoTracking()
        .AnyAsync(i => i.Id == req.ImageId && i.ProductId == req.ProductId, ct);
    if (!exists) throw new InvalidOperationException("Image not found for this product.");

    var p = await _db.Set<Product>().FirstOrDefaultAsync(x => x.Id == req.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

    _db.Entry(p).Property(nameof(Product.MainImageId)).CurrentValue = req.ImageId;
    await _db.SaveChangesAsync(ct);
    return Unit.Value;
}
}