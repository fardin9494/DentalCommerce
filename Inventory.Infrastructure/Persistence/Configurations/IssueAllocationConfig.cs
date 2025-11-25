using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class IssueAllocationConfig : IEntityTypeConfiguration<IssueAllocation>
{
    public void Configure(EntityTypeBuilder<IssueAllocation> b)
    {
        b.ToTable("IssueAllocation");
        b.HasKey(x => x.Id);

        b.Property(x => x.Qty).HasPrecision(18, 3).IsRequired();

        b.HasIndex(x => new { x.IssueLineId, x.StockItemId });

        b.HasOne<IssueLine>()
            .WithMany(l => l.Allocations)
            .HasForeignKey(x => x.IssueLineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}   