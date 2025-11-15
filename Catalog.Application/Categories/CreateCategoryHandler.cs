using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

public sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly DbContext _db;
    public CreateCategoryHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(CreateCategoryCommand req, CancellationToken ct)
    {
 
        if (req.ParentId is Guid pid)
        {
            var parentExists = await _db.Set<Category>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == pid, ct);

            if (!parentExists)
                throw new InvalidOperationException("Parent category not found.");
        }

        var cat = Category.Create(
            name: req.Name.Trim(),
            defaultSlug: req.Slug.Trim().ToLowerInvariant(),
            parentId: req.ParentId
        );

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            _db.Set<Category>().Add(cat);
            await _db.SaveChangesAsync(ct);

            _db.Set<CategoryClosure>().Add(CategoryClosure.Self(cat.Id));

            if (req.ParentId is Guid parentId)
            {
                var ancestors = await _db.Set<CategoryClosure>()
                    .AsNoTracking()
                    .Where(cc => cc.DescendantId == parentId)
                    .ToListAsync(ct);

                foreach (var a in ancestors)
                {
                    _db.Set<CategoryClosure>().Add(
                        CategoryClosure.Link(a.AncestorId, cat.Id, a.Depth + 1)
                    );
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return cat.Id;
    }
}
