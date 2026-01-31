using MediatR;
using Pricing.Domain.Common;
using Pricing.Domain.Coupons;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Features.Coupons;

public sealed record CreateCouponCommand(
    string Code,
    string? Name,
    bool IsActive,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int? MaxUsesTotal,
    int? MaxUsesPerUser,
    int Priority,
    bool CombinableWithPromotions,
    string? ExclusiveGroup,
    Guardrails? Guardrails,
    EligibilityDefinition Eligibility,
    BenefitDefinition Benefit) : IRequest<Guid>;

public sealed class CreateCouponHandler : IRequestHandler<CreateCouponCommand, Guid>
{
    private readonly Abstractions.IPricingDbContext _db;

    public CreateCouponHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateCouponCommand request, CancellationToken ct)
    {
        var entity = Coupon.Create(
            request.Code,
            request.Name,
            request.IsActive,
            request.ValidFrom,
            request.ValidTo,
            request.MaxUsesTotal,
            request.MaxUsesPerUser,
            request.Priority,
            request.CombinableWithPromotions,
            request.ExclusiveGroup,
            request.Guardrails,
            request.Eligibility,
            request.Benefit);

        _db.Coupons.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
