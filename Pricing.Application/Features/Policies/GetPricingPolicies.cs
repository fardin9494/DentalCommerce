using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Pricing.Application.Features.Policies;

public sealed record GetPricingPoliciesQuery : IRequest<IReadOnlyList<PricingPolicyDto>>;

public sealed class GetPricingPoliciesHandler : IRequestHandler<GetPricingPoliciesQuery, IReadOnlyList<PricingPolicyDto>>
{
    private readonly Abstractions.IPricingDbContext _db;

    public GetPricingPoliciesHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<IReadOnlyList<PricingPolicyDto>> Handle(GetPricingPoliciesQuery request, CancellationToken ct)
    {
        var entities = await _db.PricingPolicies
            .AsNoTracking()
            .ToListAsync(ct);

        return entities
            .Select(GetPricingPolicyHandler.Map)
            .ToList();
    }
}
