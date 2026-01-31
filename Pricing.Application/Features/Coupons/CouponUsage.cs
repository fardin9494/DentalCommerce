using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Pricing.Application.Features.Coupons;

public sealed record GetCouponUsageQuery(Guid CouponId, Guid? UserId) : IRequest<CouponUsageDto?>;

public sealed record CouponUsageDto(
    Guid CouponId,
    int RedeemedTotal,
    int ReservedActiveTotal,
    int RedeemedByUser,
    int ReservedActiveByUser);

public sealed class GetCouponUsageHandler : IRequestHandler<GetCouponUsageQuery, CouponUsageDto?>
{
    private readonly Abstractions.IPricingDbContext _db;
    private readonly Abstractions.IClock _clock;

    public GetCouponUsageHandler(Abstractions.IPricingDbContext db, Abstractions.IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CouponUsageDto?> Handle(GetCouponUsageQuery request, CancellationToken ct)
    {
        var exists = await _db.Coupons.AsNoTracking().AnyAsync(x => x.Id == request.CouponId, ct);
        if (!exists) return null;

        var now = _clock.UtcNow;
        var baseQuery = _db.CouponRedemptions.AsNoTracking()
            .Where(x => x.CouponId == request.CouponId);

        var redeemedTotal = await baseQuery.CountAsync(x => x.RedeemedAt != null, ct);
        var reservedActiveTotal = await baseQuery.CountAsync(x => x.RedeemedAt == null && x.ExpiresAt.HasValue && x.ExpiresAt > now, ct);

        var redeemedByUser = 0;
        var reservedActiveByUser = 0;
        if (request.UserId.HasValue)
        {
            redeemedByUser = await baseQuery.CountAsync(x => x.UserId == request.UserId && x.RedeemedAt != null, ct);
            reservedActiveByUser = await baseQuery.CountAsync(x => x.UserId == request.UserId && x.RedeemedAt == null && x.ExpiresAt.HasValue && x.ExpiresAt > now, ct);
        }

        return new CouponUsageDto(
            request.CouponId,
            redeemedTotal,
            reservedActiveTotal,
            redeemedByUser,
            reservedActiveByUser);
    }
}
