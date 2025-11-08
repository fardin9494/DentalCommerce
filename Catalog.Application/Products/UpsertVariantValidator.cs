using FluentValidation;

namespace Catalog.Application.Products;

public sealed class UpsertVariantValidator : AbstractValidator<UpsertVariantCommand>
{
    public UpsertVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantValue).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
    }
}