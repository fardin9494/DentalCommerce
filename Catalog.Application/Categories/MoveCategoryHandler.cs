using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

public sealed class MoveCategoryHandler : IRequestHandler<MoveCategoryCommand, Unit>
{
    private readonly DbContext _db;
    public MoveCategoryHandler(DbContext db) => _db = db;

    // تیپ‌های کمکی برای Projection از EF
    private sealed record AncRow(Guid AncestorId, int Depth);
    private sealed record DescRow(Guid DescendantId, int Depth);

    public async Task<Unit> Handle(MoveCategoryCommand req, CancellationToken ct)
    {
        var cat = await _db.Set<Category>().FirstOrDefaultAsync(c => c.Id == req.CategoryId, ct);
        if (cat is null) throw new InvalidOperationException("Category not found.");

        // اگر والد جدید همان والد فعلی است، کاری نکن
        if (cat.ParentId == req.NewParentId) return Unit.Value;

        // جلوگیری اضافه (در کنار Validator): انتقال زیرِ خودِ نود یا یکی از نوادگانش ممنوع
        if (req.NewParentId is Guid pid)
        {
            if (pid == req.CategoryId)
                throw new InvalidOperationException("Cannot move a category under itself.");

            var isDescendant = await _db.Set<CategoryClosure>()
                .AsNoTracking()
                .AnyAsync(cc => cc.AncestorId == req.CategoryId && cc.DescendantId == pid, ct);

            if (isDescendant)
                throw new InvalidOperationException("Cannot move category under its own descendant.");
        }

        // همهٔ نوادگانِ این نود (شامل خودش با depth=0)
        var descendants = await _db.Set<CategoryClosure>()
            .AsNoTracking()
            .Where(cc => cc.AncestorId == req.CategoryId)
            .Select(cc => new DescRow(cc.DescendantId, cc.Depth))
            .ToListAsync(ct);

        // همهٔ نیاکان قدیمیِ این نود (شامل خودش با depth=0)
        var oldAncestors = await _db.Set<CategoryClosure>()
            .AsNoTracking()
            .Where(cc => cc.DescendantId == req.CategoryId)
            .Select(cc => new AncRow(cc.AncestorId, cc.Depth))
            .ToListAsync(ct);

        // نیاکان والد جدید (اگر والد دارد)، شامل خودش با depth=0
        List<AncRow> newAncestors = new();
        if (req.NewParentId is Guid newPid)
        {
            newAncestors = await _db.Set<CategoryClosure>()
                .AsNoTracking()
                .Where(cc => cc.DescendantId == newPid)
                .Select(cc => new AncRow(cc.AncestorId, cc.Depth))
                .ToListAsync(ct);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1) تغییر ParentId خودِ دسته
        cat.SetParent(req.NewParentId);
        await _db.SaveChangesAsync(ct);

        // 2) حذف مسیرهای قدیمی: از هر ancestor قدیمی → به هر descendant این زیر‌درخت
        var descendantIds = descendants.Select(d => d.DescendantId).ToHashSet();
        var oldAncestorIds = oldAncestors.Select(a => a.AncestorId).ToHashSet();

        var toDelete = await _db.Set<CategoryClosure>()
            .Where(cc => oldAncestorIds.Contains(cc.AncestorId) && descendantIds.Contains(cc.DescendantId))
            .ToListAsync(ct);

        _db.Set<CategoryClosure>().RemoveRange(toDelete);
        await _db.SaveChangesAsync(ct);

        // 3) افزودن مسیرهای جدید
        var inserts = new List<CategoryClosure>();

        // 3-الف) self-link برای خودِ نود (ممکن است در مرحلهٔ حذف پاک شده باشد)
        inserts.Add(CategoryClosure.Self(req.CategoryId));

        // 3-ب) لینک‌های Category → همهٔ زیرشاخه‌هایش (شامل خودش با عمق 0)
        foreach (var d in descendants)
            inserts.Add(CategoryClosure.Link(req.CategoryId, d.DescendantId, d.Depth));

        // 3-ج) اگر والد جدید دارد: لینک‌های (تمام نیاکان والد جدید) → (تمام نوادگان این زیر‌درخت)
        if (req.NewParentId is Guid)
        {
            foreach (var na in newAncestors)
            {
                foreach (var d in descendants)
                {
                    inserts.Add(CategoryClosure.Link(
                        na.AncestorId,
                        d.DescendantId,
                        na.Depth + 1 + d.Depth
                    ));
                }
            }
        }

        // حذف رکوردهای تکراری احتمالی
        inserts = inserts
            .GroupBy(x => new { x.AncestorId, x.DescendantId, x.Depth })
            .Select(g => g.First())
            .ToList();

        await _db.Set<CategoryClosure>().AddRangeAsync(inserts, ct);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        });
        return Unit.Value;
    }
}
