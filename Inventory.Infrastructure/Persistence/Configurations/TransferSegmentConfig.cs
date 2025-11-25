using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class TransferSegmentConfig : IEntityTypeConfiguration<TransferSegment>
{
    public void Configure(EntityTypeBuilder<TransferSegment> b)
    {
        b.ToTable("TransferSegment");
        b.HasKey(x => x.Id);

        b.Property(x => x.Qty).HasPrecision(18, 3).IsRequired();
        b.Property(x => x.ReceivedQty).HasPrecision(18, 3).IsRequired();

        b.HasIndex(x => new { x.TransferLineId, x.StockItemId });

        b.HasOne<TransferLine>()
            .WithMany(l => l.Segments)
            .HasForeignKey(x => x.TransferLineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}