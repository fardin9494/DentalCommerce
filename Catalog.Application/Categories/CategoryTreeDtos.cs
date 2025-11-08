namespace Catalog.Application.Categories;

public sealed class CategoryTreeNodeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public Guid? ParentId { get; init; }
    public int Depth { get; init; }
}