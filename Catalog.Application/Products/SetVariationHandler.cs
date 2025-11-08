using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class SetVariationHandler : IRequestHandler<SetVariationCommand, Unit>
{
    private readonly DbContext _db;
    public SetVariationHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(SetVariationCommand req, CancellationToken ct)
    {
        var p = await _db.Set<Product>()
            .Include(x => x.Variants) // برای پاک شدن وریانت‌ها در صورت null کردن
            .FirstOrDefaultAsync(x => x.Id == req.ProductId, ct);
        if (p is null) throw new InvalidOperationException("Product not found.");

        p.SetVariation(req.VariationKey); // null => None و وریانت‌ها Clear می‌شوند
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}