using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class TransferLineConfig : IEntityTypeConfiguration<TransferLine>
{
    public void Configure(EntityTypeBuilder<TransferLine> b)
    {
        b.ToTable("TransferLine");
        b.HasKey(x => x.Id);

        b.Property(x => x.LineNo).IsRequired();
        b.Property(x => x.ProductId).IsRequired();
        b.Property(x => x.RequestedQty).HasPrecision(18, 3).IsRequired();

        b.HasIndex(x => new { x.TransferId, x.LineNo }).IsUnique();

        b.HasOne<Transfer>()
            .WithMany(t => t.Lines)
            .HasForeignKey(x => x.TransferId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Metadata.FindNavigation(nameof(TransferLine.Segments))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}