using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class ProductPropertyConfig : IEntityTypeConfiguration<ProductProperty>
{
    public void Configure(EntityTypeBuilder<ProductProperty> b)
    {
        b.ToTable("ProductProperty");
        b.HasKey(x => x.Id);

        b.Property(x => x.Key).IsRequired().HasMaxLength(128);
        b.Property(x => x.ValueDecimal).HasPrecision(18, 4);
        b.HasIndex(x => new { x.ProductId, x.Key }).IsUnique();

        b.HasOne<Product>()
            .WithMany(p => p.Properties)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}