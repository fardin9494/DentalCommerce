using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record ListPropertyKeysQuery(int Top = 20) : IRequest<IReadOnlyList<PropertyKeyDto>>;

public sealed class PropertyKeyDto
{
    public string Key { get; init; } = default!;
    public int UsageCount { get; init; }
}

public sealed class ListPropertyKeysHandler : IRequestHandler<ListPropertyKeysQuery, IReadOnlyList<PropertyKeyDto>>
{
    private readonly DbContext _db;
    public ListPropertyKeysHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<PropertyKeyDto>> Handle(ListPropertyKeysQuery q, CancellationToken ct)
    {
        var take = q.Top <= 0 ? 20 : Math.Min(q.Top, 100);

        return await _db.Set<ProductProperty>()
            .AsNoTracking()
            .GroupBy(p => p.Key)
            .Select(g => new PropertyKeyDto { Key = g.Key, UsageCount = g.Count() })
            .OrderByDescending(x => x.UsageCount)
            .ThenBy(x => x.Key)
            .Take(take)
            .ToListAsync(ct);
    }
}
