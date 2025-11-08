// ListStores.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Stores;

namespace Catalog.Application.Stores;

public sealed record ListStoresQuery(string? Search = null) : IRequest<IReadOnlyList<StoreListItemDto>>;
public sealed class StoreListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Domain { get; init; }
}

public sealed class ListStoresHandler : IRequestHandler<ListStoresQuery, IReadOnlyList<StoreListItemDto>>
{
    private readonly DbContext _db;
    public ListStoresHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreListItemDto>> Handle(ListStoresQuery q, CancellationToken ct)
    {
        var src = _db.Set<Store>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            src = src.Where(x => x.Name.ToLower().Contains(s) || (x.Domain != null && x.Domain.ToLower().Contains(s)));
        }

        return await src
            .OrderBy(x => x.Name)
            .Select(x => new StoreListItemDto { Id = x.Id, Name = x.Name, Domain = x.Domain })
            .ToListAsync(ct);
    }
}