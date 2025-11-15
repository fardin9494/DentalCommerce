
using Catalog.Application.Medias;
using Catalog.Domain.Brands;
using Catalog.Domain.Products;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IO;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    public Guid SeededProductId { get; private set; }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1) DbContext را به InMemory تغییر بده
            services.RemoveAll(typeof(DbContextOptions<CatalogDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(CatalogDbContext));

            var dbRoot = new InMemoryDatabaseRoot();
            var dbName = "it-" + Guid.NewGuid().ToString("N");
            services.AddSingleton(dbRoot);
            services.AddDbContext<CatalogDbContext>(o =>
                o.UseInMemoryDatabase(dbName, dbRoot));

            // 2) فایل‌سیستم موقت برای Media
            var temp = Path.Combine(Path.GetTempPath(), "catalog_media_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(temp);

            services.AddScoped<IFileStorage>(_ => new LocalFileStorage(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string?>("Media:Root", temp),
                        new KeyValuePair<string, string?>("Media:BaseUrl", "http://localhost/media")
                    })
                    .Build()
            ));

            // 3) Seed داده با ServiceProvider موقت (بدون دستکاری پایپ‌لاین اپلیکیشن)
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            var brand = Brand.Create("TestBrand", "IR");
            db.Set<Brand>().Add(brand);

            var product = Product.Create(
                name: "محصول تست",
                defaultSlug: "test-product",
                code: $"T-{Guid.NewGuid():N}".Substring(0, 8),
                brandId: brand.Id
            );
            product.SetStatus(ProductStatus.Active);

            db.Set<Product>().Add(product);
            db.SaveChanges();

            SeededProductId = product.Id;
        });
    }
}
