using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class ProductCategoryConfig : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> b)
    {
        b.ToTable("ProductCategory");
        b.HasKey(x => new { x.ProductId, x.CategoryId });

        b.Property(x => x.IsPrimary).IsRequired();

        b.HasIndex(x => new { x.ProductId, x.IsPrimary })
            .HasFilter("[IsPrimary] = 1")
            .IsUnique(); // هر محصول حداکثر یک Primary

        b.HasOne<Catalog.Domain.Products.Product>()
            .WithMany(p => p.Categories)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Catalog.Domain.Categories.Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}