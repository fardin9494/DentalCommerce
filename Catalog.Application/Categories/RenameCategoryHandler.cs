using Catalog.Application.Categories;
using Catalog.Domain.Categories;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class RenameCategoryHandler : IRequestHandler<RenameCategoryCommand, Unit>
{
    private readonly DbContext _db;
    public RenameCategoryHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(RenameCategoryCommand req, CancellationToken ct)
    {
        var cat = await _db.Set<Category>().FirstOrDefaultAsync(c => c.Id == req.CategoryId, ct);
        if (cat is null) throw new InvalidOperationException("Category not found.");

        // فرض: Rename فقط نام را می‌گیرد
        cat.Rename(req.Name.Trim());

        // اسلاگ را با متد جداگانه ست کن (اگر چنین متدی داری)
        // اگر نامش فرق می‌کند (SetSlug/ChangeSlug/UpdateSlug) همان را صدا بزن
        cat.SetSlug(req.Slug.Trim().ToLowerInvariant());

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}