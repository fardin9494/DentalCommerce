using BuildingBlocks.Domain;

namespace Pricing.Domain.PriceLists;

public sealed class PriceList : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<PriceListItem> _items = new();
    public IReadOnlyCollection<PriceListItem> Items => _items;

    private PriceList() { }

    public static PriceList Create(string name, string currency, DateTime? validFrom, DateTime? validTo, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required.");

        return new PriceList
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            ValidFrom = validFrom,
            ValidTo = validTo,
            IsActive = isActive
        };
    }

    public void Update(string name, string currency, DateTime? validFrom, DateTime? validTo, bool isActive)
    {
        Name = name.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsActive = isActive;
        Touch();
    }

    public PriceListItem UpsertItem(string skuId, decimal basePrice, IReadOnlyList<TierPrice>? tierPrices = null)
    {
        if (string.IsNullOrWhiteSpace(skuId)) throw new ArgumentException("SkuId required.");

        var item = _items.FirstOrDefault(x => x.SkuId == skuId);
        if (item is null)
        {
            item = PriceListItem.Create(Id, skuId.Trim(), basePrice, tierPrices);
            _items.Add(item);
        }
        else
        {
            item.Update(basePrice, tierPrices);
        }

        Touch();
        return item;
    }
}
