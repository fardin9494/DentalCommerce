using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class CreateCountryValidator : AbstractValidator<CreateCountryCommand>
{
    public CreateCountryValidator(DbContext db)
    {
        RuleFor(x => x.Code2).NotEmpty().Length(2);
        RuleFor(x => x.Code3).NotEmpty().Length(3);
        RuleFor(x => x.NameFa).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(128);

        RuleFor(x => x).CustomAsync(async (r, ctx, ct) =>
        {
            var code2 = r.Code2.ToUpperInvariant();
            var code3 = r.Code3.ToUpperInvariant();

            var dup2 = await db.Set<Country>().AnyAsync(c => c.Code2 == code2, ct);
            if (dup2) ctx.AddFailure("Code2", "کشوری با همین Code2 وجود دارد.");

            var dup3 = await db.Set<Country>().AnyAsync(c => c.Code3 == code3, ct);
            if (dup3) ctx.AddFailure("Code3", "کشوری با همین Code3 وجود دارد.");
        });
    }
}