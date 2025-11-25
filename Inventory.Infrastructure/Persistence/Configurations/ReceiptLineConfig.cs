using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class ReceiptLineConfig : IEntityTypeConfiguration<ReceiptLine>
{
    public void Configure(EntityTypeBuilder<ReceiptLine> b)
    {
        b.ToTable("ReceiptLine");
        b.HasKey(x => x.Id);

        b.Property(x => x.LineNo).IsRequired();
        b.Property(x => x.ProductId).IsRequired();
        b.Property(x => x.Qty).HasPrecision(18, 3).IsRequired();
        b.Property(x => x.UnitCost).HasPrecision(18, 2);
        b.Property(x => x.LotNumber).HasMaxLength(64);
        b.Property(x => x.ExpiryDate).HasColumnType("datetime2");

        b.HasIndex(x => new { x.ReceiptId, x.LineNo }).IsUnique();

        b.HasOne<Receipt>()
            .WithMany(r => r.Lines)
            .HasForeignKey(x => x.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade); // حذف رسید → خطوط هم حذف می‌شوند (فقط در Draft عملاً مجاز است)
    }
}
