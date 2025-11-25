using BuildingBlocks.Domain; // AggregateRoot<Guid>
namespace Inventory.Domain.Aggregates;

/// <summary>
/// رکورد تجمیعی موجودی برای یک محصول/واریانت/انبار/لات/تاریخ انقضا
/// </summary>
public sealed class StockItem : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public Guid WarehouseId { get; private set; }

    public string? LotNumber { get; private set; }          // اختیاری: برای اقلام بدون لات خالی می‌ماند
    public DateTime? ExpiryDate { get; private set; }       // اختیاری

    public decimal OnHand { get; private set; }             // موجودی انبار
    public decimal Reserved { get; private set; }           // رزرو شده (برای سفارش)
    public decimal Blocked { get; private set; }            // مسدود/قرنطینه
    public string? BlockReason { get; private set; }

    private StockItem() { }

    public static StockItem Create(Guid productId, Guid? variantId, Guid warehouseId, string? lotNumber, DateTime? expiry)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(),
            ExpiryDate = expiry,
            OnHand = 0,
            Reserved = 0,
            Blocked = 0
        };

    public void Increase(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        OnHand += qty;
        Touch();
    }

    public void Decrease(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > Available) throw new InvalidOperationException("موجودی کافی نیست.");
        OnHand -= qty;
        Touch();
    }

    public void Reserve(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > Available) throw new InvalidOperationException("موجودی کافی برای رزرو نیست.");
        Reserved += qty;
        Touch();
    }

    public void Release(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > Reserved) throw new InvalidOperationException("رزرو کافی برای آزادسازی نیست.");
        Reserved -= qty;
        Touch();
    }

    public void Block(decimal qty, string reason)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > Available) throw new InvalidOperationException("موجودی کافی برای مسدودسازی نیست.");
        Blocked += qty;
        BlockReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Touch();
    }

    public void Unblock(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > Blocked) throw new InvalidOperationException("مقدار مسدود کافی برای آزادسازی نیست.");
        Blocked -= qty;
        if (Blocked == 0) BlockReason = null;
        Touch();
    }

    public decimal Available => OnHand - Reserved - Blocked;
}
