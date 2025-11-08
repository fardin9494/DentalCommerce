namespace Catalog.Domain.Products;

public sealed class ProductProperty
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Key { get; private set; } = null!;
    public string? ValueString { get; private set; }
    public decimal? ValueDecimal { get; private set; }
    public bool? ValueBool { get; private set; }
    public string? ValueJson { get; private set; }

    private ProductProperty() { }

    public static ProductProperty Create(Guid productId, string key, string? s, decimal? d, bool? b, string? j)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Key = key.Trim(),
            ValueString = s,
            ValueDecimal = d,
            ValueBool = b,
            ValueJson = j
        };

    public void Set(string? s, decimal? d, bool? b, string? j)
    {
        ValueString = s;
        ValueDecimal = d;
        ValueBool = b;
        ValueJson = j;
    }
}