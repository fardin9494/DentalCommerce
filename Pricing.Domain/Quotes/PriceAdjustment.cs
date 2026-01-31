namespace Pricing.Domain.Quotes;

public sealed record PriceAdjustment(
    string SourceType,
    string SourceId,
    string Description,
    decimal Amount,
    bool IsCashback = false);
