using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class ProductSeoConfig : IEntityTypeConfiguration<ProductSeo>
{
    public void Configure(EntityTypeBuilder<ProductSeo> b)
    {
        b.ToTable("ProductSeo");
        b.HasKey(x => x.Id);

        b.Property(x => x.MetaTitle).HasMaxLength(70);       // توصیه سئو
        b.Property(x => x.MetaDescription).HasMaxLength(320);
        b.Property(x => x.CanonicalUrl).HasMaxLength(512);
        b.Property(x => x.Robots).HasMaxLength(64);

        b.HasIndex(x => new { x.ProductId, x.StoreId }).IsUnique();

        b.HasOne<Product>()
            .WithMany(p => p.Seos)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Catalog.Domain.Stores.Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}