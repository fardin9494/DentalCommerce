using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed record DeleteProductPropertyCommand(Guid ProductId, Guid PropertyId) : IRequest<Unit>;

public sealed class DeleteProductPropertyHandler : IRequestHandler<DeleteProductPropertyCommand, Unit>
{
    private readonly DbContext _db;
    public DeleteProductPropertyHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteProductPropertyCommand req, CancellationToken ct)
    {
        var prop = await _db.Set<ProductProperty>()
            .FirstOrDefaultAsync(p => p.Id == req.PropertyId && p.ProductId == req.ProductId, ct);
        if (prop is null) throw new InvalidOperationException("Property not found.");

        _db.Remove(prop);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

