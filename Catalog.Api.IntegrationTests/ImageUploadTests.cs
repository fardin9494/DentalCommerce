using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Catalog.Domain.Brands;
using Catalog.Domain.Products;
using Catalog.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using SixLabors.ImageSharp;

public class ImageUploadTests : IClassFixture<CustomWebAppFactory>
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;
    private readonly CustomWebAppFactory _factory;

    public ImageUploadTests(CustomWebAppFactory f, ITestOutputHelper output)
    {
        _factory = f;
        _client = f.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task UploadImage_Should_Return201_AndId()
    {
        var productId = _factory.SeededProductId; // ✅ همون محصول seed شده

        using var content = new MultipartFormDataContent();
        var bytes = CreatePngBytes(300, 300);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "test.png"); // نام فیلد: file
        content.Add(new StringContent("alt sample"), "alt");

        var res = await _client.PostAsync($"/api/catalog/products/{productId}/images/upload", content);

        var body = await res.Content.ReadAsStringAsync();
        _output.WriteLine($"STATUS {(int)res.StatusCode} {res.StatusCode}");
        _output.WriteLine(body);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static byte[] CreatePngBytes(int w, int h)
    {
        using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(w, h);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }


private async Task<Guid> CreateProductAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // 1) برند بساز (برای رعایت FK)
        var brand = Brand.Create("TestBrand", "IR");
        db.Set<Brand>().Add(brand);

        // 2) محصول بساز (طبق امضای دامین خودت)
        var product = Product.Create(
            name: "محصول تست",
            defaultSlug: "test-product",
            code: $"T-{Guid.NewGuid():N}".Substring(0, 8),
            brandId: brand.Id
        );

        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();

        return product.Id;
    }
}
