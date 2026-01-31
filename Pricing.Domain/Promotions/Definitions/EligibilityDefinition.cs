namespace Pricing.Domain.Promotions.Definitions;

public abstract class EligibilityDefinition
{
    public string Kind { get; init; }

    protected EligibilityDefinition(string kind)
    {
        Kind = kind;
    }
}

public sealed class AllEligibilityDefinition : EligibilityDefinition
{
    public AllEligibilityDefinition() : base("all") { }
}

public sealed class AllOfEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<EligibilityDefinition> Conditions { get; init; } = Array.Empty<EligibilityDefinition>();

    public AllOfEligibilityDefinition() : base("allOf") { }
}

public sealed class AnyOfEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<EligibilityDefinition> Conditions { get; init; } = Array.Empty<EligibilityDefinition>();

    public AnyOfEligibilityDefinition() : base("anyOf") { }
}

public sealed class ProductEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<string> SkuIds { get; init; } = Array.Empty<string>();

    public ProductEligibilityDefinition() : base("product") { }
}

public sealed class CategoryEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<Guid> CategoryIds { get; init; } = Array.Empty<Guid>();

    public CategoryEligibilityDefinition() : base("category") { }
}

public sealed class BrandEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<Guid> BrandIds { get; init; } = Array.Empty<Guid>();

    public BrandEligibilityDefinition() : base("brand") { }
}

public sealed class TagEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public TagEligibilityDefinition() : base("tag") { }
}

public sealed class SiteEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<Guid> SiteIds { get; init; } = Array.Empty<Guid>();

    public SiteEligibilityDefinition() : base("site") { }
}

public sealed class UserEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<Guid> UserIds { get; init; } = Array.Empty<Guid>();

    public UserEligibilityDefinition() : base("user") { }
}

public sealed class SeasonalEligibilityDefinition : EligibilityDefinition
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }

    public SeasonalEligibilityDefinition() : base("seasonal") { }
}

public sealed class BundleEligibilityDefinition : EligibilityDefinition
{
    public IReadOnlyList<BundleRequirement> Requirements { get; init; } = Array.Empty<BundleRequirement>();

    public BundleEligibilityDefinition() : base("bundle") { }
}

public sealed class BatchExpiryBeforeEligibilityDefinition : EligibilityDefinition
{
    public DateTime? Date { get; init; }
    public int? WithinDays { get; init; }

    public BatchExpiryBeforeEligibilityDefinition() : base("batchExpiryBefore") { }
}

public readonly record struct BundleRequirement(string SkuId, int Qty);
