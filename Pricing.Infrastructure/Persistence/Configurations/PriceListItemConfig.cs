using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.PriceLists;
using Pricing.Infrastructure.Persistence.Converters;

namespace Pricing.Infrastructure.Persistence.Configurations;

public sealed class PriceListItemConfig : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> b)
    {
        b.ToTable("PriceListItem");
        b.HasKey(x => x.Id);

        b.Property(x => x.PriceListId).IsRequired();
        b.Property(x => x.SkuId).HasMaxLength(64).IsRequired();
        b.Property(x => x.BasePrice).HasColumnType("decimal(18,4)").IsRequired();

        var converter = new JsonValueConverter<IReadOnlyList<TierPrice>>(DefinitionJson.Options);
        var comparer = new JsonValueComparer<IReadOnlyList<TierPrice>>(DefinitionJson.Options);

        b.Property(x => x.TierPrices)
            .HasConversion(converter)
            .Metadata.SetValueComparer(comparer);

        b.HasIndex(x => x.SkuId);
        b.HasIndex(x => x.PriceListId);
    }
}
