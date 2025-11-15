using Catalog.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public sealed class MediaAssetConfig : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.ToTable("MediaAsset");
        b.HasKey(x => x.Id);

        b.Property(x => x.StoredPath).IsRequired().HasMaxLength(512);
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
        b.Property(x => x.ThumbsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");
    }
}
