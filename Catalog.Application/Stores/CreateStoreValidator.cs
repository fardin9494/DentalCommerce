// CreateStoreValidator.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Stores;

namespace Catalog.Application.Stores;

public sealed class CreateStoreValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreValidator(DbContext db)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Domain).MaximumLength(256);

        RuleFor(x => x).CustomAsync(async (r, ctx, ct) =>
        {
            if (!string.IsNullOrWhiteSpace(r.Domain))
            {
                var d = r.Domain.Trim().ToLower();
                var dup = await db.Set<Store>().AnyAsync(s => s.Domain == d, ct);
                if (dup) ctx.AddFailure("Domain", "این دامنه قبلاً ثبت شده است.");
            }
        });
    }
}