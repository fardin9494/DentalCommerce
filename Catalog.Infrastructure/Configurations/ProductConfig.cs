using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class ProductConfig : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        // Table & Key
        b.ToTable("Product");
        b.HasKey(x => x.Id);

        // Scalar props
        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        b.Property(x => x.DefaultSlug)
            .IsRequired()
            .HasMaxLength(256);

        b.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(64);

        b.Property(x => x.WarehouseCode)
            .HasMaxLength(64);

        b.Property(x => x.VariationKey)
            .HasMaxLength(128);

        b.Property(x => x.Status)
            .IsRequired();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        // Indexes & uniqueness
        b.HasIndex(x => x.DefaultSlug).IsUnique();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.BrandId);
        b.Property(x => x.PrimaryCategoryId);
        b.HasIndex(x => x.PrimaryCategoryId);
 
        // Relationships
        b.HasOne<Brand>()                       // Product → Brand (required)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Category>()                    // Product → PrimaryCategory (optional)
            .WithMany()
            .HasForeignKey(x => x.PrimaryCategoryId)
            .OnDelete(DeleteBehavior.SetNull);  // nullable: Guid? PrimaryCategoryId

        b.HasOne<ProductImage>()                // Product.MainImageId → ProductImage (optional)
            .WithMany()
            .HasForeignKey(x => x.MainImageId)
            .OnDelete(DeleteBehavior.NoAction); // برای جلوگیری از multiple cascade paths

        // Navigation collections: field-backed
        b.Navigation(p => p.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(p => p.Properties).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(p => p.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(p => p.Stores).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(p => p.Seos).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(p => p.Categories).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
