using BuildingBlocks.Domain;

namespace Inventory.Domain.Aggregates;

public sealed class Warehouse : AggregateRoot<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private Warehouse() { }

    public static Warehouse Create(string code, string name)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim()
        };

    public void Rename(string name)
    {
        Name = name.Trim();
        Touch(); // از Base
    }

    public void Activate() { IsActive = true; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
}
