using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class InventoryCostConfig : IEntityTypeConfiguration<InventoryCost>
{
    public void Configure(EntityTypeBuilder<InventoryCost> builder)
    {
        builder.ToTable("InventoryCosts"); // نام جدول جدید
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(3);

        // ایندکس برای جستجوی سریع هزینه یک آیتم
        builder.HasIndex(x => x.StockItemId);
    }
}