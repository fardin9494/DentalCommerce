using BuildingBlocks.Domain;

namespace Catalog.Domain.Brands;

public enum BrandStatus { Active = 1, Inactive = 2, Deprecated = 3 }

public sealed class Brand : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!; // FK -> Country.Code2
    public Guid? LogoMediaId { get; private set; }
    public int? EstablishedYear { get; private set; }
    public string? Website { get; private set; }
    public string? Description { get; private set; }
    public BrandStatus Status { get; private set; } = BrandStatus.Active;

    private Brand() { }

    public static Brand Create(string name, string countryCode, string? website = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("برند الزامی است.");

        return new Brand
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            NormalizedName = Normalize(name),
            CountryCode = countryCode.ToUpperInvariant(),
            Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim()
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("اسم برند الزامیست");
        Name = name.Trim();
        NormalizedName = Normalize(name);
        Touch();
    }

    public void SetProfile(string? description, int? establishedYear, Guid? logoMediaId, string? website)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        EstablishedYear = establishedYear;
        LogoMediaId = logoMediaId;
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
        Touch();
    }

    public void SetStatus(BrandStatus status)
    {
        Status = status;
        Touch();
    }

    private static string Normalize(string input)
    {
        // ساده: LowerInvariant + trim + جایگزینی حروف عربی/فارسی رایج
        var x = input.Trim().ToLowerInvariant()
            .Replace('ي', 'ی')
            .Replace('ك', 'ک');
        return x;
    }
}
