using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence.Configurations;

public sealed class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Order");
        b.HasKey(x => x.Id);

        b.Property(x => x.SiteId).IsRequired();
        b.Property(x => x.UserId).IsRequired(false);
        b.Property(x => x.OrderNumber).HasMaxLength(32).IsRequired();
        b.Property(x => x.PricingQuoteId).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.PlacedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.CancelledAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.PaymentFailedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.ShippedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.DeliveredAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.ReturnedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.RefundedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.PaymentFailureReason).HasMaxLength(512).IsRequired(false);
        b.Property(x => x.PaymentFailureDetails).HasColumnType("nvarchar(max)").IsRequired(false);

        b.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.DiscountTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.FinalTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.CashbackTotal).HasColumnType("decimal(18,2)");

        b.HasIndex(x => new { x.SiteId, x.PlacedAtUtc });
        b.HasIndex(x => x.PricingQuoteId);
        b.HasIndex(x => x.OrderNumber).IsUnique();

        b.Metadata.FindNavigation(nameof(Order.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(Order.Timeline))!.SetPropertyAccessMode(PropertyAccessMode.Field);

        // RowVersion توسط ConfigureRowVersion در SalesDbContext پیکربندی می‌شود
    }
}
