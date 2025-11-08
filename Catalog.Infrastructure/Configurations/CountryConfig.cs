using Catalog.Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class CountryConfig : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> b)
    {
        b.ToTable("Country");
        b.HasKey(x => x.Code2);
        b.Property(x => x.Code2).HasMaxLength(2).IsFixedLength().IsRequired();
        b.Property(x => x.Code3).HasMaxLength(3).IsFixedLength().IsRequired();
        b.Property(x => x.NameFa).HasMaxLength(128).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(128).IsRequired();
        b.Property(x => x.Region).HasMaxLength(64);
        b.Property(x => x.FlagEmoji).HasMaxLength(8);

        b.HasIndex(x => x.Code3).IsUnique();
        b.HasIndex(x => x.NameEn);
    }
}