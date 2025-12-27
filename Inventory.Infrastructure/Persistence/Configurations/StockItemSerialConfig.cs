using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockItemSerialConfig : IEntityTypeConfiguration<StockItemSerial>
{
    public void Configure(EntityTypeBuilder<StockItemSerial> b)
    {
        b.ToTable("StockItemSerial");
        b.HasKey(x => x.Id);

        b.Property(x => x.SerialNumber).HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired().HasDefaultValue(StockSerialStatus.Draft);

        b.HasIndex(x => x.SerialNumber).IsUnique();
        b.HasIndex(x => x.ReceiptLineId);
        b.HasIndex(x => x.StockItemId);
        b.HasIndex(x => x.TransferId);
        b.HasIndex(x => x.TransferLineId);
        b.HasIndex(x => x.TransferSegmentId);

        b.HasOne<ReceiptLine>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<StockItem>()
            .WithMany()
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne<Issue>()
            .WithMany()
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne<IssueLine>()
            .WithMany()
            .HasForeignKey(x => x.IssueLineId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne<Transfer>()
            .WithMany()
            .HasForeignKey(x => x.TransferId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne<TransferLine>()
            .WithMany()
            .HasForeignKey(x => x.TransferLineId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne<TransferSegment>()
            .WithMany()
            .HasForeignKey(x => x.TransferSegmentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
