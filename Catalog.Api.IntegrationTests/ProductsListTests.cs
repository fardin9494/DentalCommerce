using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

public class ProductsListTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;
    public ProductsListTests(CustomWebAppFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Get_Products_Should_Return200_And_Items()
    {
        var res = await _client.GetAsync("/api/catalog/products?page=1&pageSize=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var s = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(s);
        doc.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        items.ValueKind.Should().Be(JsonValueKind.Array);
    }
}

