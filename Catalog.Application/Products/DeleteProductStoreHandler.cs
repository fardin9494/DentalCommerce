using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class DeleteProductStoreHandler : IRequestHandler<DeleteProductStoreCommand, Unit>
{
    private readonly DbContext _db;
    public DeleteProductStoreHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteProductStoreCommand req, CancellationToken ct)
    {
        var link = await _db.Set<ProductStore>().FirstOrDefaultAsync(x => x.ProductId == req.ProductId && x.StoreId == req.StoreId, ct);
        if (link is null) return Unit.Value; // idempotent
        _db.Set<ProductStore>().Remove(link);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

