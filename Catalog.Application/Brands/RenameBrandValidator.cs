using Catalog.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Brands;

public sealed class RenameBrandValidator : AbstractValidator<RenameBrandCommand>
{
    public RenameBrandValidator(DbContext db)
    {
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);

        RuleFor(x => x).CustomAsync(async (r, ctx, ct) =>
        {
            var exists = await db.Set<Domain.Brands.Brand>().AnyAsync(b => b.Id == r.BrandId, ct);
            if (!exists) { ctx.AddFailure("BrandId", "برند یافت نشد."); return; }

            var normalized = BrandNormalize.Normalize(r.Name);
            var dup = await db.Set<Domain.Brands.Brand>()
                .AnyAsync(b => b.NormalizedName == normalized && b.Id != r.BrandId, ct);
            if (dup) ctx.AddFailure("Name", "نام انتخابی با برند دیگری تداخل دارد.");
        });
    }
}