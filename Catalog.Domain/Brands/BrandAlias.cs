namespace Catalog.Domain.Brands;

public sealed class BrandAlias
{
    public Guid Id { get; private set; }
    public Guid BrandId { get; private set; }
    public string Alias { get; private set; } = null!;
    public string? Locale { get; private set; } // "fa-IR", "en-US", ...

    private BrandAlias() { }

    public static BrandAlias Create(Guid brandId, string alias, string? locale = null)
        => new()
        {
            Id = Guid.NewGuid(),
            BrandId = brandId,
            Alias = alias.Trim(),
            Locale = string.IsNullOrWhiteSpace(locale) ? null : locale
        };
}