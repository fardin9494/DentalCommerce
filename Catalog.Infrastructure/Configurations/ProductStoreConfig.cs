using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class ProductStoreConfig : IEntityTypeConfiguration<ProductStore>
{
    public void Configure(EntityTypeBuilder<ProductStore> b)
    {
        b.ToTable("ProductStore");
        b.HasKey(x => x.Id);

        b.Property(x => x.Slug).IsRequired().HasMaxLength(256);
        b.Property(x => x.TitleOverride).HasMaxLength(256);

        b.HasIndex(x => new { x.StoreId, x.Slug }).IsUnique();
        b.HasIndex(x => new { x.ProductId, x.StoreId }).IsUnique();

        b.HasOne<Product>()
            .WithMany(p => p.Stores)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Catalog.Domain.Stores.Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}