using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.Overrides;

namespace Pricing.Infrastructure.Persistence.Configurations;

public sealed class PriceOverrideConfig : IEntityTypeConfiguration<PriceOverride>
{
    public void Configure(EntityTypeBuilder<PriceOverride> b)
    {
        b.ToTable("PriceOverride");
        b.HasKey(x => x.Id);

        b.Property(x => x.ScopeType).HasConversion<int>().IsRequired();
        b.Property(x => x.ScopeId).IsRequired();
        b.Property(x => x.SkuId).HasMaxLength(64).IsRequired();
        b.Property(x => x.OverrideType).HasConversion<int>().IsRequired();
        b.Property(x => x.Value).HasColumnType("decimal(18,4)").IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.ValidFrom).HasColumnType("datetime2");
        b.Property(x => x.ValidTo).HasColumnType("datetime2");
        b.Property(x => x.Priority).IsRequired();
        b.Property(x => x.StackingGroup).HasMaxLength(64);

        b.HasIndex(x => x.SkuId);
        b.HasIndex(x => new { x.ScopeType, x.ScopeId });
    }
}
