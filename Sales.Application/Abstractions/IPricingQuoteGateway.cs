namespace Sales.Application.Abstractions;

public interface IPricingQuoteGateway
{
    Task<PricingQuoteSnapshot> CreateQuoteAsync(PricingQuoteRequest request, CancellationToken ct);
}

public sealed record PricingQuoteRequest(
    Guid SiteId,
    Guid? UserId,
    string? CouponCode,
    IReadOnlyList<PricingQuoteRequestItem> Items);

public sealed record PricingQuoteRequestItem(
    string SkuId,
    int Qty,
    Guid? BatchId);

public sealed record PricingQuoteSnapshot(
    Guid QuoteId,
    Guid SiteId,
    Guid? UserId,
    string Currency,
    DateTime TimestampUtc,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal FinalTotal,
    decimal CashbackTotal,
    IReadOnlyList<PricingQuoteLineSnapshot> Lines);

public sealed record PricingQuoteLineSnapshot(
    string SkuId,
    Guid? BatchId,
    int Quantity,
    decimal BaseUnitPrice,
    decimal FinalUnitPrice,
    bool IsGift,
    string? AdjustmentsJson);

