using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.Common;
using Pricing.Domain.Coupons;
using Pricing.Domain.Promotions.Definitions;
using Pricing.Infrastructure.Persistence.Converters;

namespace Pricing.Infrastructure.Persistence.Configurations;

public sealed class CouponConfig : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.ToTable("Coupon");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(128);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.ValidFrom).HasColumnType("datetime2");
        b.Property(x => x.ValidTo).HasColumnType("datetime2");
        b.Property(x => x.MaxUsesTotal).IsRequired(false);
        b.Property(x => x.MaxUsesPerUser).IsRequired(false);
        b.Property(x => x.Priority).IsRequired();
        b.Property(x => x.CombinableWithPromotions).IsRequired();
        b.Property(x => x.ExclusiveGroup).HasMaxLength(64);

        var guardrailConverter = new JsonValueConverter<Guardrails?>(DefinitionJson.Options);
        var guardrailComparer = new JsonValueComparer<Guardrails?>(DefinitionJson.Options);

        b.Property(x => x.Guardrails)
            .HasConversion(guardrailConverter)
            .Metadata.SetValueComparer(guardrailComparer);

        var eligibilityConverter = new JsonValueConverter<EligibilityDefinition>(DefinitionJson.Options);
        var eligibilityComparer = new JsonValueComparer<EligibilityDefinition>(DefinitionJson.Options);

        b.Property(x => x.Eligibility)
            .HasConversion(eligibilityConverter)
            .Metadata.SetValueComparer(eligibilityComparer);

        var benefitConverter = new JsonValueConverter<BenefitDefinition>(DefinitionJson.Options);
        var benefitComparer = new JsonValueComparer<BenefitDefinition>(DefinitionJson.Options);

        b.Property(x => x.Benefit)
            .HasConversion(benefitConverter)
            .Metadata.SetValueComparer(benefitComparer);

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.IsActive, x.ValidFrom, x.ValidTo });
    }
}
