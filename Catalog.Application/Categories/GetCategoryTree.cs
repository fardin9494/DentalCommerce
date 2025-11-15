using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;

namespace Catalog.Application.Categories;

public sealed record GetCategoryTreeQuery() : IRequest<IReadOnlyList<CategoryTreeNodeDto>>;

public sealed class GetCategoryTreeHandler : IRequestHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryTreeNodeDto>>
{
    private readonly DbContext _db;
    public GetCategoryTreeHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryTreeNodeDto>> Handle(GetCategoryTreeQuery req, CancellationToken ct)
    {
        var q =
            from c in _db.Set<Category>().AsNoTracking()
            select new CategoryTreeNodeDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.DefaultSlug,
                ParentId = c.ParentId,
                Depth = _db.Set<CategoryClosure>()
                    .Where(cc => cc.DescendantId == c.Id)
                    .Join(
                        _db.Set<Category>(),
                        cc => cc.AncestorId,
                        a => a.Id,
                        (cc, a) => new { cc, a }
                    )
                    .Where(x => x.a.ParentId == null)
                    .OrderBy(x => x.cc.Depth)
                    .Select(x => x.cc.Depth)
                    .FirstOrDefault(),
                            HasProducts = _db.Set<ProductCategory>().Any(pc => pc.CategoryId == c.Id)};       var list = await q
            .OrderBy(n => n.ParentId.HasValue)
            .ThenBy(n => n.Name)
            .ToListAsync(ct);

        return list;
    }

}