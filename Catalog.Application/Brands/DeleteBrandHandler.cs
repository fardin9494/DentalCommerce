using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;
using Catalog.Domain.Products;

namespace Catalog.Application.Brands;

public sealed class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand, Unit>
{
    private readonly DbContext _db;
    public DeleteBrandHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteBrandCommand req, CancellationToken ct)
    {
        var inUse = await _db.Set<Product>().AsNoTracking().AnyAsync(p => p.BrandId == req.BrandId, ct);
        if (inUse) throw new InvalidOperationException("امکان حذف وجود ندارد؛ این برند در برخی محصولات استفاده شده است.");

        var b = await _db.Set<Brand>().FirstOrDefaultAsync(x => x.Id == req.BrandId, ct);
        if (b is null) return Unit.Value;

        _db.Set<Brand>().Remove(b);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}