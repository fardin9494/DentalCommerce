
using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockItemPriceConfig : IEntityTypeConfiguration<StockItemPrice>
{
    public void Configure(EntityTypeBuilder<StockItemPrice> b)
    {
        b.ToTable("StockItemPrice", InventoryDbContext.DefaultSchema, t =>
        {
            t.HasCheckConstraint("CK_StockItemPrice_Positive", "[Amount] > 0");
            t.HasCheckConstraint("CK_StockItemPrice_Range", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });

        b.HasKey(x => x.Id);

        b.Property(x => x.StockItemId).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.EffectiveFrom).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.EffectiveTo).HasColumnType("datetime2");

        b.HasIndex(x => x.StockItemId);
        b.HasIndex(x => new { x.StockItemId, x.EffectiveFrom }).IsUnique();
    }
}
