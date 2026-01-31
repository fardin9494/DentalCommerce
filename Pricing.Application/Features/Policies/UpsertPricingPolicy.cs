using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Common;
using Pricing.Domain.Policies;

namespace Pricing.Application.Features.Policies;

public sealed record UpsertPricingPolicyCommand(Guid? SiteId, StackingMode DefaultStackingMode) : IRequest<Guid>;

public sealed class UpsertPricingPolicyHandler : IRequestHandler<UpsertPricingPolicyCommand, Guid>
{
    private readonly Abstractions.IPricingDbContext _db;

    public UpsertPricingPolicyHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertPricingPolicyCommand request, CancellationToken ct)
    {
        var entity = await _db.PricingPolicies.FirstOrDefaultAsync(x => x.SiteId == request.SiteId, ct);

        if (entity is null)
        {
            entity = PricingPolicy.Create(request.SiteId, request.DefaultStackingMode);
            _db.PricingPolicies.Add(entity);
        }
        else
        {
            entity.Update(request.DefaultStackingMode);
        }

        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
