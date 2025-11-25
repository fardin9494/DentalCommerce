using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class IssueLineConfig : IEntityTypeConfiguration<IssueLine>
{
    public void Configure(EntityTypeBuilder<IssueLine> b)
    {
        b.ToTable("IssueLine");
        b.HasKey(x => x.Id);

        b.Property(x => x.LineNo).IsRequired();
        b.Property(x => x.ProductId).IsRequired();
        b.Property(x => x.RequestedQty).HasPrecision(18, 3).IsRequired();

        b.HasIndex(x => new { x.IssueId, x.LineNo }).IsUnique();

        b.HasOne<Issue>()
            .WithMany(i => i.Lines)
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Metadata.FindNavigation(nameof(IssueLine.Allocations))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}