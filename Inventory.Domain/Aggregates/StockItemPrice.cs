using BuildingBlocks.Domain;

namespace Inventory.Domain.Aggregates;

/// <summary>
/// قیمت پایه‌ی فروش به‌ازای یک StockItem با بازه‌ی مؤثره.
/// توجه: با تخفیف‌ها ترکیب می‌شه، ولی خودش تخفیف نیست.
/// </summary>
public sealed class StockItemPrice : BaseEntity<Guid>
{
    public Guid StockItemId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!; // e.g. "IRR" یا "IRT"
    public DateTime EffectiveFrom { get; private set; }   // UTC (inclusive)
    public DateTime? EffectiveTo { get; private set; }    // UTC (exclusive, nullable = open-ended)

    private StockItemPrice() { }

    public static StockItemPrice Create(Guid stockItemId, decimal amount, string currency,
        DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        if (stockItemId == Guid.Empty) throw new ArgumentException("StockItemId required.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required.");

        var from = DateTime.SpecifyKind(fromUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        var to = toUtc.HasValue ? DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc) : (DateTime?)null;

        if (to is not null && to < from)
            throw new ArgumentException("EffectiveTo must be >= EffectiveFrom (exclusive end).");

        return new StockItemPrice
        {
            Id = Guid.NewGuid(),
            StockItemId = stockItemId,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            EffectiveFrom = from,
            EffectiveTo = to
        };
    }

    /// <summary>بستن رکورد فعلی در زمان مشخص (پایان بازه‌ی مؤثر).</summary>
    public void CloseAt(DateTime toUtc)
    {
        var to = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);
        if (to < EffectiveFrom) throw new InvalidOperationException("CloseAt must be >= EffectiveFrom.");
        EffectiveTo = to; // ما انتهای بازه را exclusive در نظر می‌گیریم.
        Touch();
    }

    public bool IsActiveAt(DateTime atUtc)
    {
        var at = DateTime.SpecifyKind(atUtc, DateTimeKind.Utc);
        return EffectiveFrom <= at && (EffectiveTo == null || at < EffectiveTo);
    }
}
