namespace Pricing.Domain.PriceLists;

public readonly record struct TierPrice(int MinQty, decimal UnitPrice);
