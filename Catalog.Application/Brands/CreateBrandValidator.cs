using Catalog.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class CreateBrandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandValidator(DbContext db)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);

        RuleFor(x => x).CustomAsync(async (r, ctx, ct) =>
        {
            var cc = r.CountryCode.ToUpperInvariant();
            var countryExists = await db.Set<Country>().AnyAsync(c => c.Code2 == cc, ct);
            if (!countryExists) ctx.AddFailure("CountryCode", "کشور یافت نشد.");

            var normalized = BrandNormalize.Normalize(r.Name);
            var dup = await db.Set<Domain.Brands.Brand>().AnyAsync(b => b.NormalizedName == normalized, ct);
            if (dup) ctx.AddFailure("Name", "برندی با همین نام/نرمالایز وجود دارد.");
        });
    }
}