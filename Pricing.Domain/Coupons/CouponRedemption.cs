using BuildingBlocks.Domain;

namespace Pricing.Domain.Coupons;

public sealed class CouponRedemption : BaseEntity<Guid>
{
    public Guid CouponId { get; private set; }
    public string CouponCode { get; private set; } = null!;
    public Guid? UserId { get; private set; }
    public Guid? SiteId { get; private set; }
    public string? CartHash { get; private set; }
    public Guid? ReservationId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? RedeemedAt { get; private set; }
    public Guid? OrderId { get; private set; }

    private CouponRedemption() { }

    public static CouponRedemption CreateReservation(
        Guid couponId,
        string couponCode,
        Guid? userId,
        Guid? siteId,
        string? cartHash,
        Guid reservationId,
        DateTime expiresAt)
    {
        return new CouponRedemption
        {
            Id = Guid.NewGuid(),
            CouponId = couponId,
            CouponCode = couponCode.Trim().ToUpperInvariant(),
            UserId = userId,
            SiteId = siteId,
            CartHash = string.IsNullOrWhiteSpace(cartHash) ? null : cartHash.Trim(),
            ReservationId = reservationId,
            ExpiresAt = expiresAt
        };
    }

    public void MarkRedeemed(Guid? orderId, DateTime redeemedAt)
    {
        RedeemedAt = redeemedAt;
        OrderId = orderId;
        Touch();
    }
}
