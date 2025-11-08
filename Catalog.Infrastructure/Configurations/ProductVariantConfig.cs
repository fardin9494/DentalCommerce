using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class ProductVariantConfig : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.ToTable("ProductVariant");
        b.HasKey(x => x.Id);

        b.Property(x => x.Value).IsRequired().HasMaxLength(256);
        b.Property(x => x.Sku).IsRequired().HasMaxLength(64);

        b.HasIndex(x => new { x.ProductId, x.Value }).IsUnique();

        b.HasIndex(x => new { x.ProductId, x.Sku }).IsUnique();

        b.HasOne<Product>()
            .WithMany(p => p.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}