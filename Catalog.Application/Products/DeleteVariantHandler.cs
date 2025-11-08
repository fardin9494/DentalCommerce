using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class DeleteVariantHandler : IRequestHandler<DeleteVariantCommand, Unit>
{
    private readonly DbContext _db;
    public DeleteVariantHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteVariantCommand req, CancellationToken ct)
    {
        var v = await _db.Set<ProductVariant>()
            .FirstOrDefaultAsync(x => x.Id == req.VariantId && x.ProductId == req.ProductId, ct);

        if (v is null) return Unit.Value; // idempotent

        _db.Set<ProductVariant>().Remove(v);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}