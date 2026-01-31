using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Features.Campaigns;

public sealed record UpdatePromotionCampaignCommand(
    Guid Id,
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
    BenefitDefinition Benefit) : IRequest;

public sealed class UpdatePromotionCampaignHandler : IRequestHandler<UpdatePromotionCampaignCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public UpdatePromotionCampaignHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(UpdatePromotionCampaignCommand request, CancellationToken ct)
    {
        var entity = await _db.PromotionCampaigns.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) throw new InvalidOperationException("Campaign not found.");

        entity.Update(
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

        await _db.SaveChangesAsync(ct);
    }
}

public sealed record DeletePromotionCampaignCommand(Guid Id) : IRequest;

public sealed class DeletePromotionCampaignHandler : IRequestHandler<DeletePromotionCampaignCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public DeletePromotionCampaignHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(DeletePromotionCampaignCommand request, CancellationToken ct)
    {
        var entity = await _db.PromotionCampaigns.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) return;

        _db.PromotionCampaigns.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
