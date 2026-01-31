using Pricing.Domain.Promotions;

namespace Pricing.Application.Services;

public sealed record EligibilityResult(bool IsEligible, IReadOnlySet<string> EligibleSkuIds, string? Reason);

public sealed record EligiblePromotion(PromotionCampaign Campaign, IReadOnlySet<string> EligibleSkuIds, string EffectiveStackingGroup);

public sealed record RejectedPromotion(PromotionCampaign Campaign, string Reason);

public sealed record PromotionSelectionResult(
    IReadOnlyList<EligiblePromotion> Selected,
    IReadOnlyList<RejectedPromotion> Rejected);

public sealed record AppliedSource(string SourceType, string SourceId, string Name, int Priority, string? StackingGroup);
