namespace Pricing.Domain.Promotions.Definitions;

public abstract class BenefitDefinition
{
    public string Kind { get; init; }

    protected BenefitDefinition(string kind)
    {
        Kind = kind;
    }
}

public sealed class PercentOffBenefitDefinition : BenefitDefinition
{
    public decimal Percent { get; init; }

    public PercentOffBenefitDefinition(decimal percent = 0m) : base("percentOff")
    {
        Percent = percent;
    }
}

public sealed class AmountOffBenefitDefinition : BenefitDefinition
{
    public decimal Amount { get; init; }
    public string? Currency { get; init; }

    public AmountOffBenefitDefinition(decimal amount = 0m, string? currency = null) : base("amountOff")
    {
        Amount = amount;
        Currency = currency;
    }
}

public sealed class FixedPriceBenefitDefinition : BenefitDefinition
{
    public decimal Price { get; init; }
    public string? Currency { get; init; }

    public FixedPriceBenefitDefinition(decimal price = 0m, string? currency = null) : base("fixedPrice")
    {
        Price = price;
        Currency = currency;
    }
}

public sealed class BundleFixedPriceBenefitDefinition : BenefitDefinition
{
    public IReadOnlyList<string> SkuIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<BundleItemRequirement> RequiredItems { get; init; } = Array.Empty<BundleItemRequirement>();
    public decimal BundlePrice { get; init; }
    public string? Currency { get; init; }

    public BundleFixedPriceBenefitDefinition() : base("bundleFixedPrice") { }
}

public readonly record struct BundleItemRequirement(string SkuId, int QtyRequired);

public sealed class BuyXGetYBenefitDefinition : BenefitDefinition
{
    public string BuySkuId { get; init; } = string.Empty;
    public int BuyQty { get; init; }
    public string GetSkuId { get; init; } = string.Empty;
    public int GetQty { get; init; }

    public BuyXGetYBenefitDefinition() : base("buyXGetY") { }
}

public sealed class CashbackPercentBenefitDefinition : BenefitDefinition
{
    public decimal Percent { get; init; }

    public CashbackPercentBenefitDefinition(decimal percent = 0m) : base("cashbackPercent")
    {
        Percent = percent;
    }
}

public sealed class CashbackAmountBenefitDefinition : BenefitDefinition
{
    public decimal Amount { get; init; }
    public string? Currency { get; init; }

    public CashbackAmountBenefitDefinition(decimal amount = 0m, string? currency = null) : base("cashbackAmount")
    {
        Amount = amount;
        Currency = currency;
    }
}
