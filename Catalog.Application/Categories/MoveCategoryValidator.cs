using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Categories;

public sealed class MoveCategoryValidator : AbstractValidator<MoveCategoryCommand>
{
    public MoveCategoryValidator(DbContext db)
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x).CustomAsync(async (req, ctx, ct) =>
        {
            var exists = await db.Set<Catalog.Domain.Categories.Category>()
                .AnyAsync(c => c.Id == req.CategoryId, ct);
            if (!exists)
            {
                ctx.AddFailure("CategoryId", "Category not found.");
                return;
            }

            if (req.NewParentId is Guid pid)
            {
                var parentExists = await db.Set<Catalog.Domain.Categories.Category>()
                    .AnyAsync(c => c.Id == pid, ct);
                if (!parentExists)
                {
                    ctx.AddFailure("NewParentId", "New parent not found.");
                    return;
                }

                // جلوگیری از چرخه: parent نباید از نوادگان Category باشد
                var cycle = await db.Set<Catalog.Domain.Categories.CategoryClosure>()
                    .AnyAsync(cc => cc.AncestorId == req.CategoryId && cc.DescendantId == pid, ct);
                if (cycle)
                    ctx.AddFailure("NewParentId", "Cannot move category under its own descendant.");
            }
        });
    }
}