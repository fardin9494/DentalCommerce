using BuildingBlocks.Domain;

namespace Pricing.Domain.Overrides;

public sealed class PriceOverride : AggregateRoot<Guid>
{
    public OverrideScope ScopeType { get; private set; }
    public Guid ScopeId { get; private set; }
    public string SkuId { get; private set; } = null!;
    public OverrideType OverrideType { get; private set; }
    public decimal Value { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public int Priority { get; private set; }
    public string? StackingGroup { get; private set; }

    private PriceOverride() { }

    public static PriceOverride Create(
        OverrideScope scopeType,
        Guid scopeId,
        string skuId,
        OverrideType overrideType,
        decimal value,
        string currency,
        DateTime? validFrom,
        DateTime? validTo,
        int priority,
        string? stackingGroup)
    {
        if (string.IsNullOrWhiteSpace(skuId)) throw new ArgumentException("SkuId required.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required.");

        return new PriceOverride
        {
            Id = Guid.NewGuid(),
            ScopeType = scopeType,
            ScopeId = scopeId,
            SkuId = skuId.Trim(),
            OverrideType = overrideType,
            Value = value,
            Currency = currency.Trim().ToUpperInvariant(),
            ValidFrom = validFrom,
            ValidTo = validTo,
            Priority = priority,
            StackingGroup = string.IsNullOrWhiteSpace(stackingGroup) ? null : stackingGroup.Trim()
        };
    }

    public void Update(
        OverrideType overrideType,
        decimal value,
        string currency,
        DateTime? validFrom,
        DateTime? validTo,
        int priority,
        string? stackingGroup)
    {
        OverrideType = overrideType;
        Value = value;
        Currency = currency.Trim().ToUpperInvariant();
        ValidFrom = validFrom;
        ValidTo = validTo;
        Priority = priority;
        StackingGroup = string.IsNullOrWhiteSpace(stackingGroup) ? null : stackingGroup.Trim();
        Touch();
    }
}
