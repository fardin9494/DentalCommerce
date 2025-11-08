using FluentValidation;

namespace Catalog.Application.Products;

public sealed class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Sort).Must(s => s is null ||
                                       new[] { "name", "-name", "created", "-created", "updated", "-updated", "code", "-code" }
                                           .Contains(s.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("sort نامعتبر است.");
    }
}