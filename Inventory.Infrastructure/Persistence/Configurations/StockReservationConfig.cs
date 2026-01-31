using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfig : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> b)
    {
        b.ToTable("StockReservation");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderId).IsRequired();
        b.Property(x => x.StockItemId).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        b.Property(x => x.Qty).HasColumnType("decimal(18,2)").IsRequired();

        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.StockItemId);
        b.HasIndex(x => new { x.OrderId, x.Sku });
    }
}

