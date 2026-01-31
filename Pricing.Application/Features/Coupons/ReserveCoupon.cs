using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Coupons;

namespace Pricing.Application.Features.Coupons;

public sealed record ReserveCouponCommand(
    string Code,
    Guid? SiteId,
    Guid? UserId,
    string? CartHash,
    int? DurationMinutes) : IRequest<ReserveCouponResult>;

public sealed record ReserveCouponResult(Guid ReservationId, DateTime ExpiresAt);

public sealed class ReserveCouponHandler : IRequestHandler<ReserveCouponCommand, ReserveCouponResult>
{
    private static readonly TimeSpan DefaultReservationTtl = TimeSpan.FromMinutes(15);

    private readonly Abstractions.IPricingDbContext _db;
    private readonly Abstractions.IClock _clock;

    public ReserveCouponHandler(Abstractions.IPricingDbContext db, Abstractions.IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ReserveCouponResult> Handle(ReserveCouponCommand request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (coupon is null) throw new InvalidOperationException("Coupon not found.");

        var now = _clock.UtcNow;
        if (!coupon.IsActive || (coupon.ValidFrom.HasValue && coupon.ValidFrom.Value > now) || (coupon.ValidTo.HasValue && coupon.ValidTo.Value < now))
            throw new InvalidOperationException("Coupon is inactive.");

        var ttl = request.DurationMinutes.HasValue
            ? TimeSpan.FromMinutes(Math.Clamp(request.DurationMinutes.Value, 1, 60))
            : DefaultReservationTtl;

        var existing = await _db.CouponRedemptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CouponId == coupon.Id
                && x.UserId == request.UserId
                && x.CartHash == request.CartHash
                && x.ReservationId != null
                && x.ExpiresAt.HasValue
                && x.ExpiresAt > now
                && x.RedeemedAt == null, ct);

        if (existing is not null)
            return new ReserveCouponResult(existing.ReservationId!.Value, existing.ExpiresAt!.Value);

        await EnsureUsageAvailableAsync(coupon, request.UserId, now, ct);

        var reservationId = Guid.NewGuid();
        var expiresAt = now.Add(ttl);
        var reservation = CouponRedemption.CreateReservation(
            coupon.Id,
            coupon.Code,
            request.UserId,
            request.SiteId,
            request.CartHash,
            reservationId,
            expiresAt);

        _db.CouponRedemptions.Add(reservation);
        await _db.SaveChangesAsync(ct);

        return new ReserveCouponResult(reservationId, expiresAt);
    }

    private async Task EnsureUsageAvailableAsync(Coupon coupon, Guid? userId, DateTime now, CancellationToken ct)
    {
        if (coupon.MaxUsesTotal is null && coupon.MaxUsesPerUser is null) return;

        var totalUsed = await _db.CouponRedemptions
            .AsNoTracking()
            .CountAsync(x => x.CouponId == coupon.Id && x.RedeemedAt != null, ct);

        var reserved = await _db.CouponRedemptions
            .AsNoTracking()
            .CountAsync(x => x.CouponId == coupon.Id && x.RedeemedAt == null && x.ExpiresAt.HasValue && x.ExpiresAt > now, ct);

        if (coupon.MaxUsesTotal.HasValue && totalUsed + reserved >= coupon.MaxUsesTotal.Value)
            throw new InvalidOperationException("Coupon usage limit reached.");

        if (coupon.MaxUsesPerUser.HasValue)
        {
            if (!userId.HasValue) throw new InvalidOperationException("Coupon requires a user.");

            var perUserUsed = await _db.CouponRedemptions
                .AsNoTracking()
                .CountAsync(x => x.CouponId == coupon.Id && x.UserId == userId && x.RedeemedAt != null, ct);

            var perUserReserved = await _db.CouponRedemptions
                .AsNoTracking()
                .CountAsync(x => x.CouponId == coupon.Id && x.UserId == userId && x.RedeemedAt == null && x.ExpiresAt.HasValue && x.ExpiresAt > now, ct);

            if (perUserUsed + perUserReserved >= coupon.MaxUsesPerUser.Value)
                throw new InvalidOperationException("Coupon usage limit reached for user.");
        }
    }
}
