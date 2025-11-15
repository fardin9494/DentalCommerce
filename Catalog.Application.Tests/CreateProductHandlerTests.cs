using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Products;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class CreateProductHandlerTests
{
    private static CatalogDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(opts);
    }

    [Fact]
    public async Task Should_Create_Product_And_Set_Primary_Category()
    {
        await using var db = NewDb();
        var brand = Brand.Create("B1", "IR");
        db.Set<Brand>().Add(brand);
        var cat1 = Category.Create("C1", "c1");
        var cat2 = Category.Create("C2", "c2");
        db.AddRange(cat1, cat2);
        await db.SaveChangesAsync();

        var handler = new CreateProductHandler(db);
        var id = await handler.Handle(new CreateProductCommand(
            Name: "P1", Slug: "p1", Code: "X-100",
            BrandId: brand.Id,
            CategoryIds: new[] { cat1.Id, cat2.Id },
            WarehouseCode: null,
            VariationKey: null
            , "IR"
        ), CancellationToken.None);

        var p = await db.Set<Catalog.Domain.Products.Product>().FindAsync(id);
        p.Should().NotBeNull();
        p!.PrimaryCategoryId.Should().Be(cat1.Id);
        p.Categories.Select(c => c.CategoryId).Should().Contain(new[] { cat1.Id, cat2.Id });
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Code()
    {
        await using var db = NewDb();
        var brand = Brand.Create("B1", "IR");
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync();

        var handler = new CreateProductHandler(db);
        var cmd = new CreateProductCommand("P1", "p1", "X-100", brand.Id, Array.Empty<Guid>(), null, null,"IR");
        await handler.Handle(cmd, CancellationToken.None);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

