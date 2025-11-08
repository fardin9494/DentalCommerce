namespace Catalog.Domain.Products;

public sealed class ProductImage
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = null!;
    public string? Alt { get; private set; }
    public int SortOrder { get; private set; }

    private ProductImage() { }

    public static ProductImage Create(Guid productId, string url, string? alt, int sortOrder)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Url = url.Trim(),
            Alt = string.IsNullOrWhiteSpace(alt) ? null : alt.Trim(),
            SortOrder = sortOrder
        };

    public void Reorder(int sortOrder) => SortOrder = sortOrder;
    public void Edit(string url, string? alt)
    {
        Url = url.Trim();
        Alt = string.IsNullOrWhiteSpace(alt) ? null : alt.Trim();
    }
}