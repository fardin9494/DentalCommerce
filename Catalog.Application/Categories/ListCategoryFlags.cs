using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;

namespace Catalog.Application.Categories;

public sealed record ListCategoryFlagsQuery() : IRequest<IReadOnlyList<CategoryFlagsDto>>;

public sealed class CategoryFlagsDto
{
    public Guid Id { get; init; }
    public bool HasProducts { get; init; }
}

public sealed class ListCategoryFlagsHandler : IRequestHandler<ListCategoryFlagsQuery, IReadOnlyList<CategoryFlagsDto>>
{
    private readonly DbContext _db;
    public ListCategoryFlagsHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryFlagsDto>> Handle(ListCategoryFlagsQuery req, CancellationToken ct)
    {
        var result = await _db.Set<Category>()
            .AsNoTracking()
            .Select(c => new CategoryFlagsDto
            {
                Id = c.Id,
                HasProducts = _db.Set<ProductCategory>().Any(pc => pc.CategoryId == c.Id)
            })
            .ToListAsync(ct);
        return result;
    }
}

