// SetStoreDomainValidator.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Stores;

namespace Catalog.Application.Stores;

public sealed class SetStoreDomainValidator : AbstractValidator<SetStoreDomainCommand>
{
    public SetStoreDomainValidator(DbContext db)
    {
        RuleFor(x => x.Domain).MaximumLength(256);

        RuleFor(x => x).CustomAsync(async (r, ctx, ct) =>
        {
            var exists = await db.Set<Store>().AnyAsync(s => s.Id == r.StoreId, ct);
            if (!exists) { ctx.AddFailure("StoreId", "Store یافت نشد."); return; }

            if (!string.IsNullOrWhiteSpace(r.Domain))
            {
                var d = r.Domain.Trim().ToLower();
                var dup = await db.Set<Store>()
                    .AnyAsync(s => s.Domain == d && s.Id != r.StoreId, ct);
                if (dup) ctx.AddFailure("Domain", "این دامنه برای استور دیگری ثبت شده است.");
            }
        });
    }
}