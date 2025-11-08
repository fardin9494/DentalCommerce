using static System.Net.Mime.MediaTypeNames;

namespace Catalog.Application.Products;

public sealed class ProductListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string DefaultSlug { get; init; } = default!;
    public Guid? BrandId { get; init; }
    public string? BrandName { get; init; }
    public Guid? PrimaryCategoryId { get; init; }
    public string Status { get; init; } = default!;
    public Guid? MainImageId { get; init; }
    public string? MainImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
public sealed class ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string DefaultSlug { get; init; } = default!;
    public string BrandName { get; init; } = default!;
    public Guid BrandId { get; init; }
    public string Status { get; init; } = default!;
    public Guid? PrimaryCategoryId { get; init; }

    public string? VariationKey { get; init; }
    public IReadOnlyList<VariantDto> Variants { get; init; } = Array.Empty<VariantDto>();
    public IReadOnlyList<PropertyDto> Properties { get; init; } = Array.Empty<PropertyDto>();
    public IReadOnlyList<ImageDto> Images { get; init; } = Array.Empty<ImageDto>();
    public IReadOnlyList<CategoryLinkDto> Categories { get; init; } = Array.Empty<CategoryLinkDto>();

    // per-store
    public IReadOnlyList<ProductStoreDto> Stores { get; init; } = Array.Empty<ProductStoreDto>();
    public IReadOnlyList<ProductSeoDto> Seos { get; init; } = Array.Empty<ProductSeoDto>();
}

public sealed class VariantDto
{
    public Guid Id { get; init; }
    public string Value { get; init; } = default!;
    public string Sku { get; init; } = default!;
    public bool IsActive { get; init; }
}

public sealed class PropertyDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = default!;
    public string? ValueString { get; init; }
    public decimal? ValueDecimal { get; init; }
    public bool? ValueBool { get; init; }
    public string? ValueJson { get; init; }
}

public sealed class ImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = default!;
    public string? Alt { get; init; }
    public int SortOrder { get; init; }
    public bool IsMain { get; init; }
}

public sealed class CategoryLinkDto
{
    public Guid CategoryId { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class ProductStoreDto
{
    public Guid StoreId { get; init; }
    public bool IsVisible { get; init; }
    public string Slug { get; init; } = default!;
    public string? TitleOverride { get; init; }
    public string? DescriptionOverride { get; init; }
}

public sealed class ProductSeoDto
{
    public Guid StoreId { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? Robots { get; init; }
    public string? JsonLd { get; init; }
}
