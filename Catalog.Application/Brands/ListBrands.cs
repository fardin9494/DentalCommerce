using Catalog.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;
using Catalog.Domain.Products;

namespace Catalog.Application.Brands;

public sealed record ListBrandsQuery(string? Search = null, BrandStatus? Status = null)
    : IRequest<IReadOnlyList<BrandListItemDto>>;

public sealed class BrandListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    // Country removed from Brand; available per-product
    public string? Website { get; init; }
    public BrandStatus Status { get; init; }
    public int ProductsCount { get; init; }
}

public sealed class ListBrandsHandler : IRequestHandler<ListBrandsQuery, IReadOnlyList<BrandListItemDto>>
{
    private readonly DbContext _db;
    public ListBrandsHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<BrandListItemDto>> Handle(ListBrandsQuery q, CancellationToken ct)
    {
        var src = _db.Set<Brand>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = BrandNormalize.Normalize(q.Search);
            src = src.Where(b => b.NormalizedName.Contains(s) || b.Name.Contains(q.Search));
        }
        // Country filter removed (moved to Product)
        if (q.Status.HasValue)
            src = src.Where(b => b.Status == q.Status.Value);

        return await src
            .OrderBy(b => b.Name)
            .Select(b => new BrandListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Website = b.Website,
                Status = b.Status,
                ProductsCount = _db.Set<Product>().Count(p => p.BrandId == b.Id)
            })
            .ToListAsync(ct);
    }
}

