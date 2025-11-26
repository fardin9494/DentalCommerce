using BuildingBlocks.Domain;

namespace Inventory.Domain.Aggregates;

/// <summary>
/// نگهداری قیمت تمام شده (خرید) برای یک آیتم در انبار.
/// این اطلاعات محرمانه است و نباید به مشتری نشان داده شود.
/// </summary>
public sealed class InventoryCost : BaseEntity<Guid>
{
    public Guid StockItemId { get; private set; }
    public decimal Amount { get; private set; } // قیمت خرید/تولید
    public string Currency { get; private set; } = null!;

    // تاریخ ثبت هزینه (می‌تواند جایگزین EffectiveFrom باشد)
    public DateTime RecordedAt { get; private set; }

    private InventoryCost() { }

    public static InventoryCost Create(Guid stockItemId, decimal amount, string currency)
    {
        if (stockItemId == Guid.Empty) throw new ArgumentException("StockItemId required.");
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); // هزینه صفر (هدیه) ممکن است
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required.");

        return new InventoryCost
        {
            Id = Guid.NewGuid(),
            StockItemId = stockItemId,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            RecordedAt = DateTime.UtcNow
        };
    }

    // متد برای اصلاح قیمت تمام شده (مثلاً اگر اشتباه وارد شده بود)
    public void CorrectCost(decimal newAmount)
    {
        if (newAmount < 0) throw new ArgumentOutOfRangeException(nameof(newAmount));
        Amount = newAmount;
        Touch();
    }
}