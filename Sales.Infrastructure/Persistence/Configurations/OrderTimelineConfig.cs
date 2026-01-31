using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence.Configurations;

public sealed class OrderTimelineConfig : IEntityTypeConfiguration<OrderTimelineEntry>
{
    public void Configure(EntityTypeBuilder<OrderTimelineEntry> b)
    {
        b.ToTable("OrderTimeline");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderId).IsRequired();
        b.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        b.Property(x => x.FromStatus).IsRequired(false);
        b.Property(x => x.ToStatus).IsRequired(false);
        b.Property(x => x.Message).HasMaxLength(512).IsRequired(false);
        b.Property(x => x.DataJson).HasColumnType("nvarchar(max)").IsRequired(false);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();

        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => new { x.OrderId, x.CreatedAt });

        b.HasOne<Order>()
            .WithMany(x => x.Timeline)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
