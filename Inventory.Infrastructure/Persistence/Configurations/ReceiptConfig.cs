using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class ReceiptConfig : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> b)
    {
        b.ToTable("Receipt");
        b.HasKey(x => x.Id);

        b.Property(x => x.WarehouseId).IsRequired();
        b.Property(x => x.DocDate).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.ExternalRef).HasMaxLength(64);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.ApprovedAt).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.ReceivedAt).HasColumnType("datetime2").IsRequired(false);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        b.HasIndex(x => new { x.WarehouseId, x.Status, x.DocDate });
        b.Metadata.FindNavigation(nameof(Receipt.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}