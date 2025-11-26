using BuildingBlocks.Domain;

namespace Inventory.Domain.Aggregates;

public sealed class StockShelf : AggregateRoot<Guid>
{
    public Guid WarehouseId { get; private set; }
    public string Name { get; private set; } = null!; // مثال: "A-01-01"
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private StockShelf() { }

    public static StockShelf Create(Guid warehouseId, string name, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Name = name.Trim(),
            Description = description,
            IsActive = true
        };

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}