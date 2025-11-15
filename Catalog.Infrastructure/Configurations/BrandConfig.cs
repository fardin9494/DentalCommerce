using Catalog.Domain.Brands;
using Catalog.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class BrandConfig : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.ToTable("Brand");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(256);
        b.Property(x => x.NormalizedName).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.NormalizedName).IsUnique();

        // Country moved to Product; brand no longer holds CountryCode
        b.Property(x => x.Website).HasMaxLength(256);
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.Property(x => x.Status).IsRequired();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        b.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.LogoMediaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
