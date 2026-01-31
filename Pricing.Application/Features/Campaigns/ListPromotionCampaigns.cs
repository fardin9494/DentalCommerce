using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Promotions;

namespace Pricing.Application.Features.Campaigns;

public sealed record ListPromotionCampaignsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<PromotionCampaignDto>>;

public sealed class ListPromotionCampaignsHandler : IRequestHandler<ListPromotionCampaignsQuery, IReadOnlyList<PromotionCampaignDto>>
{
    private readonly Abstractions.IPricingDbContext _db;

    public ListPromotionCampaignsHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<IReadOnlyList<PromotionCampaignDto>> Handle(ListPromotionCampaignsQuery request, CancellationToken ct)
    {
        var query = _db.PromotionCampaigns.AsNoTracking().AsQueryable();
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var items = await query.ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    internal static PromotionCampaignDto Map(PromotionCampaign entity)
    {
        return new PromotionCampaignDto
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Priority = entity.Priority,
            StackingGroup = entity.StackingGroup,
            StackingMode = entity.StackingMode,
            CombinableWithOtherPromotions = entity.CombinableWithOtherPromotions,
            CombinableWithCoupons = entity.CombinableWithCoupons,
            ExclusiveGroup = entity.ExclusiveGroup,
            Guardrails = entity.Guardrails,
            Eligibility = entity.Eligibility,
            Benefit = entity.Benefit
        };
    }
}

public sealed record GetPromotionCampaignByIdQuery(Guid Id) : IRequest<PromotionCampaignDto?>;

public sealed class GetPromotionCampaignByIdHandler : IRequestHandler<GetPromotionCampaignByIdQuery, PromotionCampaignDto?>
{
    private readonly Abstractions.IPricingDbContext _db;

    public GetPromotionCampaignByIdHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<PromotionCampaignDto?> Handle(GetPromotionCampaignByIdQuery request, CancellationToken ct)
    {
        var entity = await _db.PromotionCampaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? null : ListPromotionCampaignsHandler.Map(entity);
    }
}
