using Catalog.Application.Categories;
using FluentValidation;

namespace Catalog.Application.Products;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator(ICategoryReadService cats)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);

        RuleFor(x => x.CategoryIds)
            .NotNull().Must(x => x.Count > 0)
            .WithMessage("محصول باید حداقل یک دسته داشته باشد.");

        RuleForEach(x => x.CategoryIds).CustomAsync(async (catId, ctx, ct) =>
        {
            if (!await cats.ExistsAsync(catId, ct))
            {
                ctx.AddFailure("CategoryIds", $"دسته {catId} وجود ندارد.");
                return;
            }
            if (!await cats.IsLeafAsync(catId, ct))
            {
                ctx.AddFailure("CategoryIds", $"دسته {catId} برگ (Leaf) نیست.");
            }
        });

        RuleFor(x => x.VariationKey).MaximumLength(128);
    }
}