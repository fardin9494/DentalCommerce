using FluentValidation;
using FluentValidation.Validators;

namespace Inventory.Application.Common.Validators;

/// <summary>
/// Validator برای ProductId که از Catalog Context استفاده می‌کند
/// </summary>
public static class ProductIdValidator
{
    public static IRuleBuilderOptions<T, Guid> MustBeValidProductId<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        ICatalogProductService catalogService)
    {
        return ruleBuilder
            .MustAsync(async (productId, ct) => await catalogService.ProductExistsAsync(productId, ct))
            .WithMessage("محصول یافت نشد.");
    }

    public static IRuleBuilderOptions<T, Guid?> MustBeValidVariantId<T>(
        this IRuleBuilder<T, Guid?> ruleBuilder,
        Guid productId,
        ICatalogProductService catalogService)
    {
        return ruleBuilder
            .MustAsync(async (variantId, ct) =>
            {
                if (!variantId.HasValue) return true;
                return await catalogService.VariantExistsAsync(productId, variantId.Value, ct);
            })
            .WithMessage("Variant یافت نشد.");
    }
}

