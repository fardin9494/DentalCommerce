using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Inventory.Infrastructure.Persistence.Configurations;


public sealed class AdjustmentConfig : IEntityTypeConfiguration<Adjustment>
{
    public void Configure(EntityTypeBuilder<Adjustment> b)
    {
        b.ToTable("Adjustment");
        b.HasKey(x => x.Id);

        b.Property(x => x.WarehouseId).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.Reason).IsRequired();
        b.Property(x => x.DocDate).HasColumnType("datetime2");
        b.Property(x => x.PostedAt).HasColumnType("datetime2");
        b.Property(x => x.Note).HasMaxLength(512);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        b.Metadata.FindNavigation(nameof(Adjustment.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
