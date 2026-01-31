using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Pricing.Application.Features.Coupons;

public sealed record ListCouponReservationsQuery(Guid CouponId, bool ActiveOnly = true) : IRequest<IReadOnlyList<CouponReservationDto>>;

public sealed record CouponReservationDto(
    Guid Id,
    Guid? ReservationId,
    Guid? UserId,
    Guid? SiteId,
    string? CartHash,
    DateTime? ExpiresAt,
    DateTime? RedeemedAt,
    DateTime CreatedAt);

public sealed class ListCouponReservationsHandler : IRequestHandler<ListCouponReservationsQuery, IReadOnlyList<CouponReservationDto>>
{
    private readonly Abstractions.IPricingDbContext _db;
    private readonly Abstractions.IClock _clock;

    public ListCouponReservationsHandler(Abstractions.IPricingDbContext db, Abstractions.IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CouponReservationDto>> Handle(ListCouponReservationsQuery request, CancellationToken ct)
    {
        var exists = await _db.Coupons.AsNoTracking().AnyAsync(x => x.Id == request.CouponId, ct);
        if (!exists) return Array.Empty<CouponReservationDto>();

        var now = _clock.UtcNow;
        var query = _db.CouponRedemptions.AsNoTracking()
            .Where(x => x.CouponId == request.CouponId && x.ReservationId != null);

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.RedeemedAt == null && x.ExpiresAt.HasValue && x.ExpiresAt > now);
        }

        var list = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CouponReservationDto(
                x.Id,
                x.ReservationId,
                x.UserId,
                x.SiteId,
                x.CartHash,
                x.ExpiresAt,
                x.RedeemedAt,
                x.CreatedAt))
            .ToListAsync(ct);

        return list;
    }
}
