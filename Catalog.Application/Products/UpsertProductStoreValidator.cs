using FluentValidation;

namespace Catalog.Application.Products;

public sealed class UpsertProductStoreValidator : AbstractValidator<UpsertProductStoreCommand>
{
    public UpsertProductStoreValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(256);
        RuleFor(x => x.TitleOverride).MaximumLength(256);
    }
}