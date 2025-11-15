using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Categories;

public sealed record ListLeafCategoriesQuery() : IRequest<IReadOnlyList<LeafCategoryDto>>;

public sealed class LeafCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public Guid? ParentId { get; init; }
    public int Depth { get; init; }
}

public sealed class ListLeafCategoriesHandler : IRequestHandler<ListLeafCategoriesQuery, IReadOnlyList<LeafCategoryDto>>
{
    private readonly DbContext _db;
    public ListLeafCategoriesHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<LeafCategoryDto>> Handle(ListLeafCategoriesQuery q, CancellationToken ct)
    {
        // leaf: categories that have no direct children
        var leaves = await _db.Set<Domain.Categories.Category>()
            .AsNoTracking()
            .Where(c => !_db.Set<Domain.Categories.Category>().Any(ch => ch.ParentId == c.Id))
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

        return leaves;
    }
}

