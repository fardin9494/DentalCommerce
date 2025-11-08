using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class RemoveBrandAliasHandler : IRequestHandler<RemoveBrandAliasCommand, Unit>
{
    private readonly DbContext _db;
    public RemoveBrandAliasHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(RemoveBrandAliasCommand req, CancellationToken ct)
    {
        var alias = await _db.Set<BrandAlias>().FirstOrDefaultAsync(x => x.Id == req.AliasId, ct);
        if (alias is null) return Unit.Value;

        _db.Set<BrandAlias>().Remove(alias);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}