using FluentValidation;

namespace Catalog.Application.Products;

public sealed class AddProductImageValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(1024);
        RuleFor(x => x.Alt)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Alt));
    }
}