using System;
using System.Threading.Tasks;
using Catalog.Domain.Categories;
using Catalog.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class DbSmokeTests
{
    private class TestDbContext : CatalogDbContext
    {
        public TestDbContext(DbContextOptions<CatalogDbContext> opts) : base(opts) { }
    }

    [Fact]
    public async Task Can_Save_Category_And_Self_Closure()
    {
        var opts = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new TestDbContext(opts);

        var cat = Category.Create("ریشه", "root", null);
        db.Set<Category>().Add(cat);
        await db.SaveChangesAsync();

        // ✅ به‌جای new ... از factory خود دامین استفاده کن:
        var self = CategoryClosure.Link(cat.Id, cat.Id, 0);
        db.Set<CategoryClosure>().Add(self);
        await db.SaveChangesAsync();

        var cnt = await db.Set<CategoryClosure>().CountAsync();
        cnt.Should().Be(1);
    }
}