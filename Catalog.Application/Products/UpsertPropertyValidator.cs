using FluentValidation;

namespace Catalog.Application.Products;

public sealed class UpsertPropertyValidator : AbstractValidator<UpsertPropertyCommand>
{
    public UpsertPropertyValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(128);
    }
}