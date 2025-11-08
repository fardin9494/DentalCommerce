namespace Catalog.Domain.Products;

public sealed class ProductVariant
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Value { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private ProductVariant() { }

    public static ProductVariant Create(Guid productId, string value, string sku, bool isActive)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Value = value.Trim(),
            Sku = sku.Trim(),
            IsActive = isActive
        };

    public void UpdateSku(string sku) => Sku = sku.Trim();
    public void SetActive(bool active) => IsActive = active;
}