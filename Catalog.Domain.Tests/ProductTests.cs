using System;
using Catalog.Domain.Products;
using FluentAssertions;
using Xunit;

public class ProductTests
{
    [Fact]
    public void AddCategory_SetsPrimaryWhenFirst()
    {
        var p = Product.Create("کامپوزیت A", "comp-a", "C-1001", brandId: Guid.NewGuid());
        var catId = Guid.NewGuid();

        p.AddCategory(catId, makePrimary: false);
        p.PrimaryCategoryId.Should().Be(catId);
    }

    [Fact]
    public void SetPrimaryCategory_Throws_IfNotLinked()
    {
        var p = Product.Create("کامپوزیت A", "comp-a", "C-1001", brandId: Guid.NewGuid());
        var other = Guid.NewGuid();

        var act = () => p.SetPrimaryCategory(other);
        act.Should().Throw<InvalidOperationException>();
    }
}