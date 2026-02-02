using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence.Configurations;

public sealed class OrderLineConfig : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> b)
    {
        b.ToTable("OrderLine");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderId).IsRequired();
        b.Property(x => x.SkuId).HasMaxLength(64).IsRequired();
        b.Property(x => x.BatchId).IsRequired(false);
        b.Property(x => x.Quantity).IsRequired();
        b.Property(x => x.CancelledQty).IsRequired().HasDefaultValue(0);
        b.Property(x => x.ReturnedQty).IsRequired().HasDefaultValue(0);
        b.Property(x => x.RefundedQty).IsRequired().HasDefaultValue(0);
        b.Property(x => x.BaseUnitPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.FinalUnitPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.IsGift).IsRequired();
        b.Property(x => x.AdjustmentsJson).HasColumnType("nvarchar(max)").IsRequired(false);

        b.HasIndex(x => new { x.OrderId, x.SkuId });
        b.HasIndex(x => x.BatchId);

        b.HasOne<Order>()
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.OrderId);
    }
}
