using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Categories;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator(DbContext db)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(256);

        RuleFor(x => x.Slug).CustomAsync(async (slug, ctx, ct) =>
        {
            var exists = await db.Set<Catalog.Domain.Categories.Category>()
                .AnyAsync(c => c.DefaultSlug == slug, ct);
            if (exists) ctx.AddFailure("Slug", "Slug تکراری است.");
        });
    }
}