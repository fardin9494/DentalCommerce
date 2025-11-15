using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.Media;
using Catalog.Domain.Products;
using Catalog.Domain.Stores;
using Catalog.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public class CatalogDbContext : DbContext
{
    public const string DefaultSchema = "catalog";

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryClosure> CategoryClosures => Set<CategoryClosure>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<BrandAlias> BrandAliases => Set<BrandAlias>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductProperty> ProductProperties => Set<ProductProperty>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductStore> ProductStores => Set<ProductStore>();
    public DbSet<ProductSeo> ProductSeos => Set<ProductSeo>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        CountrySeed.Seed(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}
