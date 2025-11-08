namespace Catalog.Domain.Products;

public sealed class ProductSeo
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid StoreId { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? Robots { get; private set; }
    public string? JsonLd { get; private set; }

    private ProductSeo() { }

    public static ProductSeo Create(Guid productId, Guid storeId, string? title, string? desc, string? canonical, string? robots, string? jsonLd)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            StoreId = storeId,
            MetaTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            MetaDescription = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim(),
            CanonicalUrl = string.IsNullOrWhiteSpace(canonical) ? null : canonical.Trim(),
            Robots = string.IsNullOrWhiteSpace(robots) ? null : robots.Trim(),
            JsonLd = string.IsNullOrWhiteSpace(jsonLd) ? null : jsonLd.Trim()
        };

    public void Update(string? title, string? desc, string? canonical, string? robots, string? jsonLd)
    {
        MetaTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        MetaDescription = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim();
        CanonicalUrl = string.IsNullOrWhiteSpace(canonical) ? null : canonical.Trim();
        Robots = string.IsNullOrWhiteSpace(robots) ? null : robots.Trim();
        JsonLd = string.IsNullOrWhiteSpace(jsonLd) ? null : jsonLd.Trim();
    }
}