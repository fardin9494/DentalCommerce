using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence.Configurations;

public sealed class OrderRefundLineConfig : IEntityTypeConfiguration<OrderRefundLine>
{
    public void Configure(EntityTypeBuilder<OrderRefundLine> b)
    {
        b.ToTable("OrderRefundLine");
        b.HasKey(x => x.Id);

        b.Property(x => x.RefundId).IsRequired();
        b.Property(x => x.OrderLineId).IsRequired();
        b.Property(x => x.SkuId).HasMaxLength(64).IsRequired();
        b.Property(x => x.BatchId).IsRequired(false);
        b.Property(x => x.Quantity).IsRequired();
        b.Property(x => x.UnitAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.LineAmount).HasColumnType("decimal(18,2)");

        b.HasIndex(x => x.RefundId);
        b.HasIndex(x => x.OrderLineId);
    }
}
