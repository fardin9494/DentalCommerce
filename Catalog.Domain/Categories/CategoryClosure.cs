namespace Catalog.Domain.Categories;

public sealed class CategoryClosure
{
    private CategoryClosure() { }

    private CategoryClosure(Guid ancestorId, Guid descendantId, int depth)
    {
        AncestorId = ancestorId;
        DescendantId = descendantId;
        Depth = depth;
    }

    public Guid AncestorId { get; private set; }
    public Guid DescendantId { get; private set; }
    public int Depth { get; private set; }

    public static CategoryClosure Link(Guid ancestorId, Guid descendantId, int depth)
        => new CategoryClosure(ancestorId, descendantId, depth);

    public static CategoryClosure Self(Guid id) => new CategoryClosure(id, id, 0);
}