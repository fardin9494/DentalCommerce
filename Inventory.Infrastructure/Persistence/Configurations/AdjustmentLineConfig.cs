using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;


public sealed class AdjustmentLineConfig : IEntityTypeConfiguration<AdjustmentLine>
{
    public void Configure(EntityTypeBuilder<AdjustmentLine> b)
    {
        b.ToTable("AdjustmentLine");
        b.HasKey(x => x.Id);

        b.Property(x => x.LineNo).IsRequired();
        b.Property(x => x.ProductId).IsRequired();
        b.Property(x => x.QtyDelta).HasPrecision(18, 3).IsRequired();
        b.Property(x => x.ExpiryDate).HasColumnType("datetime2");
        b.Property(x => x.LotNumber).HasMaxLength(64);

        b.HasIndex(x => new { x.AdjustmentId, x.LineNo }).IsUnique();

        b.HasOne<Adjustment>()
            .WithMany(a => a.Lines)
            .HasForeignKey(x => x.AdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
