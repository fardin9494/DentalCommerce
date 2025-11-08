using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Brands;

public sealed class SetBrandProfileValidator : AbstractValidator<SetBrandProfileCommand>
{
    public SetBrandProfileValidator(DbContext db)
    {
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.EstablishedYear).InclusiveBetween(1800, DateTime.UtcNow.Year).When(x => x.EstablishedYear.HasValue);

        RuleFor(x => x.BrandId).CustomAsync(async (id, ctx, ct) =>
        {
            var exists = await db.Set<Domain.Brands.Brand>().AnyAsync(b => b.Id == id, ct);
            if (!exists) ctx.AddFailure("BrandId", "برند یافت نشد.");
        });
    }
}