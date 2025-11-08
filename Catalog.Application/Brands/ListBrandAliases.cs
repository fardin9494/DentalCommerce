using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed record ListBrandAliasesQuery(Guid BrandId) : IRequest<IReadOnlyList<BrandAliasDto>>;
public sealed class BrandAliasDto { public Guid Id { get; init; } public string Alias { get; init; } = default!; public string? Locale { get; init; } }

public sealed class ListBrandAliasesHandler : IRequestHandler<ListBrandAliasesQuery, IReadOnlyList<BrandAliasDto>>
{
    private readonly DbContext _db;
    public ListBrandAliasesHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<BrandAliasDto>> Handle(ListBrandAliasesQuery req, CancellationToken ct)
        => await _db.Set<BrandAlias>().AsNoTracking()
            .Where(a => a.BrandId == req.BrandId)
            .OrderBy(a => a.Alias)
            .Select(a => new BrandAliasDto { Id = a.Id, Alias = a.Alias, Locale = a.Locale })
            .ToListAsync(ct);
}