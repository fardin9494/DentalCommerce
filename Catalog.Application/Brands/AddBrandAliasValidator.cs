using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Brands;

public sealed class AddBrandAliasValidator : AbstractValidator<AddBrandAliasCommand>
{
    public AddBrandAliasValidator(DbContext db)
    {
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.Alias).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Locale).MaximumLength(10);

        RuleFor(x => x).CustomAsync(async (r, ctx, ct) =>
        {
            var exists = await db.Set<Domain.Brands.Brand>().AnyAsync(b => b.Id == r.BrandId, ct);
            if (!exists) ctx.AddFailure("BrandId", "برند یافت نشد.");

            var dup = await db.Set<Domain.Brands.BrandAlias>()
                .AnyAsync(a => a.BrandId == r.BrandId && a.Alias == r.Alias.Trim(), ct);
            if (dup) ctx.AddFailure("Alias", "این نام مستعار برای این برند وجود دارد.");
        });
    }
}