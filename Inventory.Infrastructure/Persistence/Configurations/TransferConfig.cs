using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class TransferConfig : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> b)
    {
        b.ToTable("Transfer");
        b.HasKey(x => x.Id);

        b.Property(x => x.SourceWarehouseId).IsRequired();
        b.Property(x => x.DestinationWarehouseId).IsRequired();
        b.Property(x => x.DocDate).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.ShippedAt).HasColumnType("datetime2");
        b.Property(x => x.CompletedAt).HasColumnType("datetime2");
        b.Property(x => x.ExternalRef).HasMaxLength(64);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        b.HasIndex(x => new { x.SourceWarehouseId, x.DestinationWarehouseId, x.Status, x.DocDate });

        b.Metadata.FindNavigation(nameof(Transfer.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}