using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Medias;
using Catalog.Application.Products;
using Catalog.Domain.Brands;
using Catalog.Domain.Products;
using Catalog.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class UploadProductImageHandlerTests
{
    private sealed class FakeImageProcessor : IImageProcessor
    {
        public Task<ProcessedImageResult> ProcessAndSaveAsync(Stream input, string originalFileName, CancellationToken ct)
            => Task.FromResult(new ProcessedImageResult
            {
                OriginalPath = "orig.webp",
                ContentType = "image/webp",
                Width = 300,
                Height = 300,
                SizeBytes = 12345,
                Thumbs = new System.Collections.Generic.Dictionary<string, string> { ["sm"] = "orig_sm.webp" }
            });

        public Task DeleteRelatedAsync(string originalPath, System.Collections.Generic.IEnumerable<string> thumbNames, CancellationToken ct)
            => Task.CompletedTask;
    }

    private static CatalogDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(opts);
    }

    [Fact]
    public async Task Should_Add_Image_And_Set_Main_When_Empty()
    {
        await using var db = NewDb();
        var brand = Brand.Create("B1", "IR");
        db.Set<Brand>().Add(brand);
        var product = Product.Create("P1", "p1", "X-1", brand.Id);
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();

        var handler = new UploadProductImageHandler(db, new FakeImageProcessor());
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var id = await handler.Handle(new UploadProductImageCommand(
            ProductId: product.Id,
            FileName: "test.png",
            ContentType: "image/png",
            Content: stream,
            Alt: "alt"
        ), CancellationToken.None);

        var img = await db.Set<ProductImage>().FindAsync(id);
        img.Should().NotBeNull();
        img!.Url.Should().Be("orig.webp");

        var reloaded = await db.Set<Product>().FindAsync(product.Id);
        reloaded!.MainImageId.Should().Be(id);
    }
}

