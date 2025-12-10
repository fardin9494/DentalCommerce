using BuildingBlocks.Domain;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Aggregates;

public sealed class StockItem : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public Guid WarehouseId { get; private set; }

    /// <summary>
    /// Denormalized SKU for self-contained inventory operations.
    /// This allows the Inventory context to identify physical items without querying the Catalog service.
    /// </summary>
    public string Sku { get; private set; } = null!;

    // تغییر اصلی: استفاده از شناسه شلف
    public Guid? ShelfId { get; private set; }

    public string? LotNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    public decimal OnHand { get; private set; }
    public decimal Reserved { get; private set; }
    public decimal Blocked { get; private set; }
    public string? BlockReason { get; private set; }
    
    /// <summary>
    /// وضعیت موجودی - محاسبه شده بر اساس OnHand, Reserved, Blocked, ShelfId
    /// </summary>
    public StockStatus Status
    {
        get
        {
            if (Blocked > 0)
            {
                // اگر BlockReason "Awaiting Shelving" باشد، وضعیت AwaitingShelving است
                if (BlockReason == "Awaiting Shelving")
                    return StockStatus.AwaitingShelving;
                return StockStatus.Blocked;
            }
            if (Reserved > 0 && Available == 0)
                return StockStatus.Reserved;
            if (ShelfId.HasValue && Available > 0)
                return StockStatus.Available;
            // اگر در قفسه نیست ولی Blocked هم نیست، باید AwaitingShelving باشد
            if (!ShelfId.HasValue && OnHand > 0 && Blocked == 0)
                return StockStatus.AwaitingShelving;
            return StockStatus.Available;
        }
    }

    private StockItem() { }

    public static StockItem Create(
        Guid productId,
        Guid? variantId,
        Guid warehouseId,
        string sku,
        string? lotNumber,
        DateTime? expiry,
        Guid? shelfId = null)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be null or empty.", nameof(sku));

        return new StockItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            Sku = sku.Trim(),
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(),
            ExpiryDate = expiry,
            ShelfId = shelfId,
            OnHand = 0,
            Reserved = 0,
            Blocked = 0
        };
    }

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

    /// <summary>
    /// کاهش موجودی از مقدار مسدود شده (برای رد کردن کالاهای قرنطینه)
    /// این متد مستقیماً از OnHand و Blocked کم می‌کند بدون نیاز به Available
    /// </summary>
    public void DecreaseFromBlocked(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > Blocked) throw new InvalidOperationException("مقدار مسدود کافی برای کاهش نیست.");
        if (qty > OnHand) throw new InvalidOperationException("موجودی کل کافی نیست.");

        OnHand -= qty;
        Blocked -= qty;
        if (Blocked == 0) BlockReason = null;
        Touch();
    }

    /// <summary>
    /// مسدود کردن موجودی مستقیماً از OnHand (بدون نیاز به Available)
    /// این متد برای مواقعی استفاده می‌شود که می‌خواهیم موجودی را از حالت تایید شده به مسدود تبدیل کنیم
    /// </summary>
    public void BlockFromOnHand(decimal qty, string reason)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > OnHand) throw new InvalidOperationException("موجودی کل کافی برای مسدودسازی نیست.");

        // ابتدا اگر Reserved است، Release می‌کنیم
        var qtyToRelease = Math.Min(qty, Reserved);
        if (qtyToRelease > 0)
        {
            Reserved -= qtyToRelease;
        }

        // سپس Block می‌کنیم
        Blocked += qty;
        BlockReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Touch();
    }

    /// <summary>
    /// تنظیم مقدار Blocked به مقدار مشخص (برای منطق ساده تایید/رد)
    /// این متد Blocked را به مقدار جدید تنظیم می‌کند و BlockReason را به‌روز می‌کند
    /// اگر Reserved است، ابتدا Release می‌کند تا Available منفی نشود
    /// </summary>
    public void SetBlocked(decimal newBlocked, string? reason = null)
    {
        if (newBlocked < 0) throw new ArgumentOutOfRangeException(nameof(newBlocked));
        if (newBlocked > OnHand) throw new InvalidOperationException("مقدار مسدود نمی‌تواند از موجودی کل بیشتر باشد.");

        // محاسبه مقدار تغییر
        var delta = newBlocked - Blocked;
        
        // اگر باید Blocked را افزایش دهیم و Reserved وجود دارد، ابتدا Release می‌کنیم
        if (delta > 0 && Reserved > 0)
        {
            var qtyToRelease = Math.Min(Reserved, delta);
            Reserved -= qtyToRelease;
        }

        Blocked = newBlocked;
        BlockReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
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
