using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockItemConfig : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> b)
    {
        b.ToTable("StockItem");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProductId).IsRequired();
        b.Property(x => x.WarehouseId).IsRequired();

        b.Property(x => x.LotNumber).HasMaxLength(64);
        b.Property(x => x.ExpiryDate);

        b.Property(x => x.OnHand).HasPrecision(18, 3);
        b.Property(x => x.Reserved).HasPrecision(18, 3);
        b.Property(x => x.Blocked).HasPrecision(18, 3);

        b.Property(x => x.BlockReason).HasMaxLength(256);

        // یکتایی رکورد تجمیعی
        b.HasIndex(x => new { x.ProductId, x.VariantId, x.WarehouseId, x.LotNumber, x.ExpiryDate })
            .IsUnique();

        // ایندکس‌های مفید
        b.HasIndex(x => new { x.WarehouseId, x.ExpiryDate });
        b.HasIndex(x => new { x.ProductId, x.VariantId });

        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");
    }
}