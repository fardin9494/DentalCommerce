using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record ListPropertyValuesQuery(string Key, int Top = 20) : IRequest<IReadOnlyList<string>>;

public sealed class ListPropertyValuesHandler : IRequestHandler<ListPropertyValuesQuery, IReadOnlyList<string>>
{
    private readonly DbContext _db;
    public ListPropertyValuesHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> Handle(ListPropertyValuesQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.Key)) return Array.Empty<string>();
        var take = q.Top <= 0 ? 20 : Math.Min(q.Top, 100);

        return await _db.Set<ProductProperty>()
            .AsNoTracking()
            .Where(p => p.Key == q.Key && p.ValueString != null && p.ValueString != "")
            .GroupBy(p => p.ValueString!)
            .Select(g => new { Value = g.Key, UsageCount = g.Count() })
            .OrderByDescending(x => x.UsageCount)
            .ThenBy(x => x.Value)
            .Take(take)
            .Select(x => x.Value)
            .ToListAsync(ct);
    }
}
