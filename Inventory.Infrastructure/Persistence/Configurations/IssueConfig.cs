using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class IssueConfig : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> b)
    {
        b.ToTable("Issue");
        b.HasKey(x => x.Id);

        b.Property(x => x.WarehouseId).IsRequired();
        b.Property(x => x.DocDate).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.ExternalRef).HasMaxLength(64);
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.PostedAt).HasColumnType("datetime2");

        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        b.HasIndex(x => new { x.WarehouseId, x.Status, x.DocDate });
        b.Metadata.FindNavigation(nameof(Issue.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}