namespace Catalog.Domain.Products;

public sealed class ProductCategory
{
    public Guid ProductId { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsPrimary { get; private set; }

    private ProductCategory() { }

    private ProductCategory(Guid productId, Guid categoryId, bool isPrimary)
    {
        ProductId = productId;
        CategoryId = categoryId;
        IsPrimary = isPrimary;
    }

    public static ProductCategory Link(Guid productId, Guid categoryId, bool isPrimary = false)
        => new(productId, categoryId, isPrimary);

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}