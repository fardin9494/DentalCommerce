using BuildingBlocks.Domain;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Domain.Promotions;

public sealed class PromotionCampaign : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public int Priority { get; private set; }
    public string? StackingGroup { get; private set; }
    public StackingMode StackingMode { get; private set; } = StackingPolicy.DefaultMode;
    public bool CombinableWithOtherPromotions { get; private set; } = true;
    public bool CombinableWithCoupons { get; private set; } = true;
    public string? ExclusiveGroup { get; private set; }

    public Guardrails? Guardrails { get; private set; }
    public EligibilityDefinition Eligibility { get; private set; } = new AllEligibilityDefinition();
    public BenefitDefinition Benefit { get; private set; } = new PercentOffBenefitDefinition(0m);

    private PromotionCampaign() { }

    public static PromotionCampaign Create(
        string name,
        bool isActive,
        DateTime? validFrom,
        DateTime? validTo,
        int priority,
        string? stackingGroup,
        StackingMode stackingMode,
        bool combinableWithOtherPromotions,
        bool combinableWithCoupons,
        string? exclusiveGroup,
        Guardrails? guardrails,
        EligibilityDefinition eligibility,
        BenefitDefinition benefit)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");

        return new PromotionCampaign
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            IsActive = isActive,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Priority = priority,
            StackingGroup = string.IsNullOrWhiteSpace(stackingGroup) ? null : stackingGroup.Trim(),
            StackingMode = stackingMode,
            CombinableWithOtherPromotions = combinableWithOtherPromotions,
            CombinableWithCoupons = combinableWithCoupons,
            ExclusiveGroup = string.IsNullOrWhiteSpace(exclusiveGroup) ? null : exclusiveGroup.Trim(),
            Guardrails = guardrails,
            Eligibility = eligibility,
            Benefit = benefit
        };
    }

    public void Update(
        string name,
        bool isActive,
        DateTime? validFrom,
        DateTime? validTo,
        int priority,
        string? stackingGroup,
        StackingMode stackingMode,
        bool combinableWithOtherPromotions,
        bool combinableWithCoupons,
        string? exclusiveGroup,
        Guardrails? guardrails,
        EligibilityDefinition eligibility,
        BenefitDefinition benefit)
    {
        Name = name.Trim();
        IsActive = isActive;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Priority = priority;
        StackingGroup = string.IsNullOrWhiteSpace(stackingGroup) ? null : stackingGroup.Trim();
        StackingMode = stackingMode;
        CombinableWithOtherPromotions = combinableWithOtherPromotions;
        CombinableWithCoupons = combinableWithCoupons;
        ExclusiveGroup = string.IsNullOrWhiteSpace(exclusiveGroup) ? null : exclusiveGroup.Trim();
        Guardrails = guardrails;
        Eligibility = eligibility;
        Benefit = benefit;
        Touch();
    }
}
