using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.PriceLists;
using Pricing.Infrastructure.Persistence.Converters;

namespace Pricing.Infrastructure.Persistence.Configurations;

public sealed class PriceListConfig : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> b)
    {
        b.ToTable("PriceList");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.ValidFrom).HasColumnType("datetime2");
        b.Property(x => x.ValidTo).HasColumnType("datetime2");
        b.Property(x => x.IsActive).IsRequired();

        b.HasIndex(x => new { x.IsActive, x.ValidFrom, x.ValidTo });
        b.Metadata.FindNavigation(nameof(PriceList.Items))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
