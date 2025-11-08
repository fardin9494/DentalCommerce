using Catalog.Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class BrandAliasConfig : IEntityTypeConfiguration<BrandAlias>
{
    public void Configure(EntityTypeBuilder<BrandAlias> b)
    {
        b.ToTable("BrandAlias");
        b.HasKey(x => x.Id);

        b.Property(x => x.Alias).IsRequired().HasMaxLength(256);
        b.Property(x => x.Locale).HasMaxLength(10);

        b.HasIndex(x => new { x.BrandId, x.Alias }).IsUnique();

        b.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}