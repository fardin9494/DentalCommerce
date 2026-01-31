using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Features.Coupons;

public sealed class CouponDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? Name { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public int? MaxUsesTotal { get; init; }
    public int? MaxUsesPerUser { get; init; }
    public int Priority { get; init; }
    public bool CombinableWithPromotions { get; init; }
    public string? ExclusiveGroup { get; init; }
    public Guardrails? Guardrails { get; init; }
    public EligibilityDefinition Eligibility { get; init; } = new AllEligibilityDefinition();
    public BenefitDefinition Benefit { get; init; } = new PercentOffBenefitDefinition(0m);
}
