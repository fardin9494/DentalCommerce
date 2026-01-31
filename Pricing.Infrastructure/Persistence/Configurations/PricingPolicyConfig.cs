using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.Policies;
using Pricing.Domain.Common;

namespace Pricing.Infrastructure.Persistence.Configurations;

public sealed class PricingPolicyConfig : IEntityTypeConfiguration<PricingPolicy>
{
    public void Configure(EntityTypeBuilder<PricingPolicy> b)
    {
        b.ToTable("PricingPolicy");
        b.HasKey(x => x.Id);

        b.Property(x => x.SiteId).IsRequired(false);
        b.Property(x => x.DefaultStackingMode).HasConversion<int>().IsRequired();

        b.HasIndex(x => x.SiteId);
    }
}
