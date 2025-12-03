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

        b.HasIndex(x => new { x.ProductId, x.VariantId, x.WarehouseId, x.LotNumber, x.ExpiryDate, x.ShelfId })
            .IsUnique();

        // SKU configuration: required, indexed for warehouse operations
        b.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(100);

        b.HasIndex(x => x.Sku)
            .HasDatabaseName("IX_StockItem_Sku");

        b.Property(x => x.LotNumber).HasMaxLength(64);
        b.Property(x => x.ExpiryDate).HasColumnType("datetime2");

        b.Property(x => x.OnHand).HasPrecision(18, 3);
        b.Property(x => x.Reserved).HasPrecision(18, 3);
        b.Property(x => x.Blocked).HasPrecision(18, 3);
        
        // RowVersion توسط ConfigureRowVersion در InventoryDbContext پیکربندی می‌شود
        // نیازی به پیکربندی explicit نیست

        // قیود (بهتر: ToTable(...HasCheckConstraint))
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Stock_OnHand_NonNeg", "[OnHand] >= 0");
            t.HasCheckConstraint("CK_Stock_Reserved_NonNeg", "[Reserved] >= 0");
            t.HasCheckConstraint("CK_Stock_Blocked_NonNeg", "[Blocked] >= 0");
        });
    }


}