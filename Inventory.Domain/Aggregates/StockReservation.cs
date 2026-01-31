using BuildingBlocks.Domain;

namespace Inventory.Domain.Aggregates;

public sealed class StockReservation : BaseEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid StockItemId { get; private set; }
    public string Sku { get; private set; } = null!;
    public decimal Qty { get; private set; }

    private StockReservation() { }

    public static StockReservation Create(Guid orderId, Guid stockItemId, string sku, decimal qty)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(orderId));
        if (stockItemId == Guid.Empty) throw new ArgumentException("StockItemId required.", nameof(stockItemId));
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("Sku required.", nameof(sku));
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));

        return new StockReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            StockItemId = stockItemId,
            Sku = sku.Trim(),
            Qty = qty
        };
    }
}

