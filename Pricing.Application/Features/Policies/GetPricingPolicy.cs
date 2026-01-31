using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Policies;

namespace Pricing.Application.Features.Policies;

public sealed record GetPricingPolicyQuery(Guid? SiteId = null) : IRequest<PricingPolicyDto?>;

public sealed class GetPricingPolicyHandler : IRequestHandler<GetPricingPolicyQuery, PricingPolicyDto?>
{
    private readonly Abstractions.IPricingDbContext _db;

    public GetPricingPolicyHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<PricingPolicyDto?> Handle(GetPricingPolicyQuery request, CancellationToken ct)
    {
        var entity = await _db.PricingPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SiteId == request.SiteId, ct);

        return entity is null ? null : Map(entity);
    }

    internal static PricingPolicyDto Map(PricingPolicy entity)
    {
        return new PricingPolicyDto
        {
            Id = entity.Id,
            SiteId = entity.SiteId,
            DefaultStackingMode = entity.DefaultStackingMode
        };
    }
}
