namespace Catalog.Domain.Brands;

public sealed class Country
{
    // ISO-3166-1 alpha-2
    public string Code2 { get; private set; } = null!;
    public string Code3 { get; private set; } = null!;
    public string NameFa { get; private set; } = null!;
    public string NameEn { get; private set; } = null!;
    public string? Region { get; private set; }
    public string? FlagEmoji { get; private set; }

    private Country() { }

    public static Country Create(string code2, string code3, string nameFa, string nameEn, string? region = null, string? flagEmoji = null)
        => new()
        {
            Code2 = code2.ToUpperInvariant(),
            Code3 = code3.ToUpperInvariant(),
            NameFa = nameFa.Trim(),
            NameEn = nameEn.Trim(),
            Region = region,
            FlagEmoji = flagEmoji
        };
}