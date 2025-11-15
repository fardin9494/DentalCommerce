using Catalog.Domain.Categories;
using FluentAssertions;
using Xunit;

public class CategoryTests
{
    [Fact]
    public void Rename_Should_UpdateName()
    {
        var c = Category.Create("ابزار", "tools", parentId: null);

        // در دامین فعلی: Rename فقط name می‌گیرد
        c.Rename("ابزار اندو");

        c.Name.Should().Be("ابزار اندو");
        // اگر متد جداگانه‌ای برای تغییر اسلاگ داری، می‌تونی این را هم تست کنی:
         c.SetSlug("endodontic-tools");  // یا c.SetSlug(...)
         c.DefaultSlug.Should().Be("endodontic-tools");
    }

    [Fact]
    public void SetParent_Should_ChangeParentId()
    {
        var root = Category.Create("ریشه", "root", null);
        var child = Category.Create("فرزند", "child", null);

        child.SetParent(root.Id);

        child.ParentId.Should().Be(root.Id);
    }
}