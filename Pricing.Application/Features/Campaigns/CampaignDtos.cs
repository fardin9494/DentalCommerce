using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Features.Campaigns;

public sealed class PromotionCampaignDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public int Priority { get; init; }
    public string? StackingGroup { get; init; }
    public StackingMode StackingMode { get; init; }
    public bool CombinableWithOtherPromotions { get; init; }
    public bool CombinableWithCoupons { get; init; }
    public string? ExclusiveGroup { get; init; }
    public Guardrails? Guardrails { get; init; }
    public EligibilityDefinition Eligibility { get; init; } = new AllEligibilityDefinition();
    public BenefitDefinition Benefit { get; init; } = new PercentOffBenefitDefinition(0m);
}
