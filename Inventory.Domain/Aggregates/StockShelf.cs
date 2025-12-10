using BuildingBlocks.Domain;

namespace Inventory.Domain.Aggregates;

public sealed class StockShelf : AggregateRoot<Guid>
{
    public Guid WarehouseId { get; private set; }
    public string Name { get; private set; } = null!; // مثال: "A-01-01"
    public string Code { get; private set; } = null!; // کد یکتا برای بارکد/اسکن
    public int RowNumber { get; private set; }       // 1-based
    public int ColumnNumber { get; private set; }    // 1-based
    public int LevelNumber { get; private set; }     // 1-based (طبقه)
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private StockShelf() { }

    public static StockShelf Create(Guid warehouseId, string name, string code, int rowNumber, int columnNumber, int levelNumber = 1, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            RowNumber = rowNumber,
            ColumnNumber = columnNumber,
            LevelNumber = levelNumber <= 0 ? 1 : levelNumber,
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

    public void Activate()
    {
        IsActive = true;
        Touch();
    }
}