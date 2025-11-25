using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockLedgerEntryConfig : IEntityTypeConfiguration<StockLedgerEntry>
{
    public void Configure(EntityTypeBuilder<StockLedgerEntry> b)
    {
        b.ToTable("StockLedger");
        b.HasKey(x => x.Id);

        b.Property(x => x.Timestamp).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.ProductId).IsRequired();
        b.Property(x => x.WarehouseId).IsRequired();

        b.Property(x => x.LotNumber).HasMaxLength(64);
        b.Property(x => x.ExpiryDate);

        b.Property(x => x.DeltaQty).HasPrecision(18, 3);
        b.Property(x => x.UnitCost).HasPrecision(18, 2);

        b.Property(x => x.RefDocType).IsRequired().HasMaxLength(64);
        b.Property(x => x.Note).HasMaxLength(512);

        b.HasIndex(x => new { x.ProductId, x.VariantId, x.WarehouseId, x.Timestamp });
        b.HasIndex(x => new { x.WarehouseId, x.LotNumber, x.ExpiryDate });
    }
}