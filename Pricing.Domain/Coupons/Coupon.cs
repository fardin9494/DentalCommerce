using BuildingBlocks.Domain;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Domain.Coupons;

public sealed class Coupon : AggregateRoot<Guid>
{
    public string Code { get; private set; } = null!;
    public string? Name { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public int? MaxUsesTotal { get; private set; }
    public int? MaxUsesPerUser { get; private set; }
    public int Priority { get; private set; }
    public bool CombinableWithPromotions { get; private set; } = true;
    public string? ExclusiveGroup { get; private set; }

    public Guardrails? Guardrails { get; private set; }
    public EligibilityDefinition Eligibility { get; private set; } = new AllEligibilityDefinition();
    public BenefitDefinition Benefit { get; private set; } = new PercentOffBenefitDefinition(0m);

    private Coupon() { }

    public static Coupon Create(
        string code,
        string? name,
        bool isActive,
        DateTime? validFrom,
        DateTime? validTo,
        int? maxUsesTotal,
        int? maxUsesPerUser,
        int priority,
        bool combinableWithPromotions,
        string? exclusiveGroup,
        Guardrails? guardrails,
        EligibilityDefinition eligibility,
        BenefitDefinition benefit)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code required.");

        return new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            IsActive = isActive,
            ValidFrom = validFrom,
            ValidTo = validTo,
            MaxUsesTotal = maxUsesTotal,
            MaxUsesPerUser = maxUsesPerUser,
            Priority = priority,
            CombinableWithPromotions = combinableWithPromotions,
            ExclusiveGroup = string.IsNullOrWhiteSpace(exclusiveGroup) ? null : exclusiveGroup.Trim(),
            Guardrails = guardrails,
            Eligibility = eligibility,
            Benefit = benefit
        };
    }

    public void Update(
        string code,
        string? name,
        bool isActive,
        DateTime? validFrom,
        DateTime? validTo,
        int? maxUsesTotal,
        int? maxUsesPerUser,
        int priority,
        bool combinableWithPromotions,
        string? exclusiveGroup,
        Guardrails? guardrails,
        EligibilityDefinition eligibility,
        BenefitDefinition benefit)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        IsActive = isActive;
        ValidFrom = validFrom;
        ValidTo = validTo;
        MaxUsesTotal = maxUsesTotal;
        MaxUsesPerUser = maxUsesPerUser;
        Priority = priority;
        CombinableWithPromotions = combinableWithPromotions;
        ExclusiveGroup = string.IsNullOrWhiteSpace(exclusiveGroup) ? null : exclusiveGroup.Trim();
        Guardrails = guardrails;
        Eligibility = eligibility;
        Benefit = benefit;
        Touch();
    }
}
