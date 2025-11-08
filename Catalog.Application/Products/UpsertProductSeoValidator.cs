using FluentValidation;

namespace Catalog.Application.Products;

public sealed class UpsertProductSeoValidator : AbstractValidator<UpsertProductSeoCommand>
{
    public UpsertProductSeoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.MetaTitle).MaximumLength(70);
        RuleFor(x => x.MetaDescription).MaximumLength(320);
        RuleFor(x => x.CanonicalUrl).MaximumLength(512);
        RuleFor(x => x.Robots).MaximumLength(64);
        // JsonLd آزاد است؛ اگر خواستی JSON validation اضافه کن.
    }
}