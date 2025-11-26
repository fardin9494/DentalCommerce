using BuildingBlocks.Domain;

namespace Inventory.Domain.Aggregates;

public sealed class StockItem : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public Guid WarehouseId { get; private set; }

    // تغییر اصلی: استفاده از شناسه شلف
    public Guid? ShelfId { get; private set; }

    public string? LotNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    public decimal OnHand { get; private set; }
    public decimal Reserved { get; private set; }
    public decimal Blocked { get; private set; }
    public string? BlockReason { get; private set; }

    public byte[] RowVersion { get; private set; } = default!;
    private StockItem() { }

    public static StockItem Create(
        Guid productId,
        Guid? variantId,
        Guid warehouseId,
        string? lotNumber,
        DateTime? expiry,
        Guid? shelfId = null) // پارامتر جدید
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(),
            ExpiryDate = expiry,
            ShelfId = shelfId,
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
        if (qty > Available) throw new InvalidOperationException("موجودی آزاد کافی نیست.");

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
        // نکته: ممکن است بخواهیم موجودی Available را چک کنیم یا کل OnHand را.
        // معمولا فقط از Available می‌توان بلاک کرد.
        if (qty > Available) throw new InvalidOperationException("موجودی آزاد کافی برای مسدودسازی نیست.");

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

    // متد انتقال بین قفسه (Bin Transfer)
    public void MoveToShelf(Guid newShelfId)
    {
        if (newShelfId == Guid.Empty) throw new ArgumentException("شناسه قفسه نامعتبر است.");
        ShelfId = newShelfId;
        Touch();
    }

    public decimal Available => OnHand - Reserved - Blocked;
}