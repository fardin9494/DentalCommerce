using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record ListVariantValuesQuery(int Top = 20) : IRequest<IReadOnlyList<VariantValueDto>>;

public sealed class VariantValueDto
{
    public string Value { get; init; } = default!;
    public string Sku { get; init; } = default!;
    public int UsageCount { get; init; }
}

public sealed class ListVariantValuesHandler : IRequestHandler<ListVariantValuesQuery, IReadOnlyList<VariantValueDto>>
{
    private readonly DbContext _db;
    public ListVariantValuesHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<VariantValueDto>> Handle(ListVariantValuesQuery q, CancellationToken ct)
    {
        var take = q.Top <= 0 ? 20 : Math.Min(q.Top, 100);

        return await _db.Set<ProductVariant>()
            .AsNoTracking()
            .GroupBy(v => new { v.Value, v.Sku })
            .Select(g => new VariantValueDto
            {
                Value = g.Key.Value,
                Sku = g.Key.Sku,
                UsageCount = g.Count()
            })
            .OrderByDescending(x => x.UsageCount)
            .ThenBy(x => x.Value)
            .Take(take)
            .ToListAsync(ct);
    }
}
