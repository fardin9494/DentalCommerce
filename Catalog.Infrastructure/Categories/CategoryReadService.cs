using Catalog.Application.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Categories;

public sealed class CategoryReadService : ICategoryReadService
{
    private readonly CatalogDbContext _db;
    public CategoryReadService(CatalogDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid categoryId, CancellationToken ct)
        => _db.Categories.AnyAsync(c => c.Id == categoryId, ct);

    public async Task<bool> IsLeafAsync(Guid categoryId, CancellationToken ct)
    {
        // Leaf = ??? ????? ??????? ????? (?? Closure depth=1 ???????? ancestor ???? ????)
        return !await _db.Categories.AnyAsync(c => c.ParentId == categoryId, ct);
    }
}
