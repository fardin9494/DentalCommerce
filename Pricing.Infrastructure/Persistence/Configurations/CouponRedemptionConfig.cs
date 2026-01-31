using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.Coupons;

namespace Pricing.Infrastructure.Persistence.Configurations;

public sealed class CouponRedemptionConfig : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> b)
    {
        b.ToTable("CouponRedemption");
        b.HasKey(x => x.Id);

        b.Property(x => x.CouponId).IsRequired();
        b.Property(x => x.CouponCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.UserId).IsRequired(false);
        b.Property(x => x.SiteId).IsRequired(false);
        b.Property(x => x.CartHash).HasMaxLength(128);
        b.Property(x => x.ReservationId).IsRequired(false);
        b.Property(x => x.ExpiresAt).HasColumnType("datetime2");
        b.Property(x => x.RedeemedAt).HasColumnType("datetime2");
        b.Property(x => x.OrderId).IsRequired(false);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2");

        b.HasIndex(x => x.CouponId);
        b.HasIndex(x => x.CouponCode);
        b.HasIndex(x => new { x.UserId, x.CouponId });
        b.HasIndex(x => x.ReservationId);
        b.HasIndex(x => x.ExpiresAt);
    }
}
