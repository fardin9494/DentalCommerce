using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Features.Coupons;

public sealed record UpdateCouponCommand(
    Guid Id,
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
    BenefitDefinition Benefit) : IRequest;

public sealed class UpdateCouponHandler : IRequestHandler<UpdateCouponCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public UpdateCouponHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(UpdateCouponCommand request, CancellationToken ct)
    {
        var entity = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) throw new InvalidOperationException("Coupon not found.");

        entity.Update(
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

        await _db.SaveChangesAsync(ct);
    }
}

public sealed record DeleteCouponCommand(Guid Id) : IRequest;

public sealed class DeleteCouponHandler : IRequestHandler<DeleteCouponCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public DeleteCouponHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(DeleteCouponCommand request, CancellationToken ct)
    {
        var entity = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) return;

        _db.Coupons.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
