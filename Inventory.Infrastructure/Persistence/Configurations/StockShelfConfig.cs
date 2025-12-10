
using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class StockShelfConfig : IEntityTypeConfiguration<StockShelf>
{
    public void Configure(EntityTypeBuilder<StockShelf> builder)
    {
        builder.ToTable("StockShelves");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RowNumber).IsRequired();
        builder.Property(x => x.ColumnNumber).IsRequired();
        builder.Property(x => x.LevelNumber).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200);

        // هر انبار شلف‌های خودش را دارد
        builder.HasIndex(x => new { x.WarehouseId, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
    }
}