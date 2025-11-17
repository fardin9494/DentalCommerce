using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record ListRecentVariantsQuery(int Top = 10) : IRequest<RecentVariantsDto>;

public sealed class RecentVariantsDto
{
    public IReadOnlyList<string> Values { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Skus { get; init; } = Array.Empty<string>();
}

public sealed class ListRecentVariantsHandler : IRequestHandler<ListRecentVariantsQuery, RecentVariantsDto>
{
    private readonly DbContext _db;
    public ListRecentVariantsHandler(DbContext db) => _db = db;

    public async Task<RecentVariantsDto> Handle(ListRecentVariantsQuery q, CancellationToken ct)
    {
        var take = q.Top <= 0 ? 10 : Math.Min(q.Top, 50);

        var recent = _db.Set<ProductVariant>()
            .AsNoTracking()
            .OrderByDescending(v => v.Id) // best-effort ordering by creation
            .Take(take * 3);

        var values = await recent
            .Select(v => v.Value)
            .Distinct()
            .Take(take)
            .ToListAsync(ct);

        var skus = await recent
            .Select(v => v.Sku)
            .Distinct()
            .Take(take)
            .ToListAsync(ct);

        return new RecentVariantsDto { Values = values, Skus = skus };
    }
}
