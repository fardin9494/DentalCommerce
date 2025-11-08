using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class ProductImageConfig : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        b.ToTable("ProductImage");
        b.HasKey(x => x.Id);

        b.Property(x => x.Url).IsRequired().HasMaxLength(1024);
        b.Property(x => x.Alt).HasMaxLength(256);
        b.Property(x => x.SortOrder).HasDefaultValue(0);

        b.HasIndex(x => x.ProductId);

        b.HasOne<Product>()
            .WithMany(p => p.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}