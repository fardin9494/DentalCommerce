using MediatR;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Features.Campaigns;

public sealed record CreatePromotionCampaignCommand(
    string Name,
    bool IsActive,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int Priority,
    string? StackingGroup,
    StackingMode StackingMode,
    bool CombinableWithOtherPromotions,
    bool CombinableWithCoupons,
    string? ExclusiveGroup,
    Guardrails? Guardrails,
    EligibilityDefinition Eligibility,
    BenefitDefinition Benefit) : IRequest<Guid>;

public sealed class CreatePromotionCampaignHandler : IRequestHandler<CreatePromotionCampaignCommand, Guid>
{
    private readonly Abstractions.IPricingDbContext _db;

    public CreatePromotionCampaignHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<Guid> Handle(CreatePromotionCampaignCommand request, CancellationToken ct)
    {
        var entity = PromotionCampaign.Create(
            request.Name,
            request.IsActive,
            request.ValidFrom,
            request.ValidTo,
            request.Priority,
            request.StackingGroup,
            request.StackingMode,
            request.CombinableWithOtherPromotions,
            request.CombinableWithCoupons,
            request.ExclusiveGroup,
            request.Guardrails,
            request.Eligibility,
            request.Benefit);

        _db.PromotionCampaigns.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
