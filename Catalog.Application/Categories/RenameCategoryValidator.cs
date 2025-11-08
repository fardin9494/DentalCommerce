using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Categories;

public sealed class RenameCategoryValidator : AbstractValidator<RenameCategoryCommand>
{
    public RenameCategoryValidator(DbContext db)
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(256);

        RuleFor(x => x).CustomAsync(async (req, ctx, ct) =>
        {
            var exists = await db.Set<Catalog.Domain.Categories.Category>()
                .AnyAsync(c => c.Id == req.CategoryId, ct);
            if (!exists) ctx.AddFailure("CategoryId", "Category not found.");

            var dup = await db.Set<Catalog.Domain.Categories.Category>()
                .AnyAsync(c => c.DefaultSlug == req.Slug && c.Id != req.CategoryId, ct);
            if (dup) ctx.AddFailure("Slug", "Slug تکراری است.");
        });
    }
}