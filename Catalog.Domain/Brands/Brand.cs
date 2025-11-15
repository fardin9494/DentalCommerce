using BuildingBlocks.Domain;

namespace Catalog.Domain.Brands;

public enum BrandStatus { Active = 1, Inactive = 2, Deprecated = 3 }

public sealed class Brand : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public Guid? LogoMediaId { get; private set; }
    public int? EstablishedYear { get; private set; }
    public string? Website { get; private set; }
    public string? Description { get; private set; }
    public BrandStatus Status { get; private set; } = BrandStatus.Active;

    private Brand() { }

    public static Brand Create(
        string name,
        string? website = null,
        string? description = null,
        int? establishedYear = null,
        Guid? logoMediaId = null,
        BrandStatus status = BrandStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");

        return new Brand
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            NormalizedName = Normalize(name),
            Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            EstablishedYear = establishedYear,
            LogoMediaId = logoMediaId,
            Status = status
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid name.");
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

    public static string Normalize(string input)
    {
        return input.Trim().ToLowerInvariant();
    }
}
