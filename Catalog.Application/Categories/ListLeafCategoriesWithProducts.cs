using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Categories;

public sealed record ListLeafCategoriesWithProductsQuery() : IRequest<IReadOnlyList<LeafCategoryDto>>;

public sealed class ListLeafCategoriesWithProductsHandler : IRequestHandler<ListLeafCategoriesWithProductsQuery, IReadOnlyList<LeafCategoryDto>>
{
    private readonly DbContext _db;
    public ListLeafCategoriesWithProductsHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<LeafCategoryDto>> Handle(ListLeafCategoriesWithProductsQuery q, CancellationToken ct)
    {
        // leaf: categories that have no direct children
        var leavesWithProducts = await _db.Set<Domain.Categories.Category>()
            .AsNoTracking()
            .Where(c => !_db.Set<Domain.Categories.Category>().Any(ch => ch.ParentId == c.Id))
            .Where(c => _db.Set<Domain.Products.ProductCategory>().Any(pc => pc.CategoryId == c.Id))
            .Select(c => new LeafCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.DefaultSlug,
                ParentId = c.ParentId,
                Depth = _db.Set<Domain.Categories.CategoryClosure>().Count(cc => cc.DescendantId == c.Id) - 1
            })
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return leavesWithProducts;
    }
}

