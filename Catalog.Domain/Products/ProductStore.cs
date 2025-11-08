namespace Catalog.Domain.Products;

public sealed class ProductStore
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid StoreId { get; private set; }
    public bool IsVisible { get; private set; }
    public string Slug { get; private set; } = null!;
    public string? TitleOverride { get; private set; }
    public string? DescriptionOverride { get; private set; }

    private ProductStore() { }

    public static ProductStore Create(Guid productId, Guid storeId, bool isVisible, string slug, string? title, string? description)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            StoreId = storeId,
            IsVisible = isVisible,
            Slug = slug.Trim().ToLowerInvariant(),
            TitleOverride = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            DescriptionOverride = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

    public void Update(bool isVisible, string slug, string? title, string? description)
    {
        IsVisible = isVisible;
        Slug = slug.Trim().ToLowerInvariant();
        TitleOverride = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        DescriptionOverride = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}