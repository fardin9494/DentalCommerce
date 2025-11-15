using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Categories;
using Catalog.Application.Products;
using FluentAssertions;

public class CreateProductValidatorTests
{
    private sealed class FakeCategoryReadService : ICategoryReadService
    {
        private readonly HashSet<Guid> _leafs = new();
        public void AddLeaf(Guid id) => _leafs.Add(id);
        public Task<bool> ExistsAsync(Guid categoryId, CancellationToken ct) => Task.FromResult(_leafs.Contains(categoryId));
        public Task<bool> IsLeafAsync(Guid categoryId, CancellationToken ct) => Task.FromResult(_leafs.Contains(categoryId));
    }

    [Fact]
    public async Task Should_Fail_When_CategoryIds_Empty()
    {
        var cats = new FakeCategoryReadService();
        var v = new CreateProductValidator(cats);
        var cmd = new CreateProductCommand(
            Name: "P1", Slug: "p1", Code: "C1", BrandId: Guid.NewGuid(),
            CategoryIds: Array.Empty<Guid>(), WarehouseCode: null, VariationKey: null,CountryCode:null);

        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Pass_With_Valid_Leaf_Category()
    {
        var cats = new FakeCategoryReadService();
        var cat = Guid.NewGuid();
        cats.AddLeaf(cat);
        var v = new CreateProductValidator(cats);
        var cmd = new CreateProductCommand(
            Name: "P1", Slug: "p1", Code: "C1", BrandId: Guid.NewGuid(),
            CategoryIds: new[] { cat }, WarehouseCode: null, VariationKey: null, "IR");

        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_VariationKey_TooLong()
    {
        var cats = new FakeCategoryReadService();
        var cat = Guid.NewGuid();
        cats.AddLeaf(cat);
        var v = new CreateProductValidator(cats);
        var tooLong = new string('x', 129);
        var cmd = new CreateProductCommand(
            Name: "P1", Slug: "p1", Code: "C1", BrandId: Guid.NewGuid(),
            CategoryIds: new[] { cat }, WarehouseCode: null, VariationKey: tooLong,"IR");

        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
    }
}

