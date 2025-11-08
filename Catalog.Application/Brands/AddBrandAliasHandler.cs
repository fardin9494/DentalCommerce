using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class AddBrandAliasHandler : IRequestHandler<AddBrandAliasCommand, Guid>
{
    private readonly DbContext _db;
    public AddBrandAliasHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(AddBrandAliasCommand req, CancellationToken ct)
    {
        var alias = BrandAlias.Create(req.BrandId, req.Alias, req.Locale);
        _db.Set<BrandAlias>().Add(alias);
        await _db.SaveChangesAsync(ct);
        return alias.Id;
    }
}