using FluentValidation;

namespace Catalog.Application.Products;

public sealed class ActivateProductValidator : AbstractValidator<ActivateProductCommand>
{
    public ActivateProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}