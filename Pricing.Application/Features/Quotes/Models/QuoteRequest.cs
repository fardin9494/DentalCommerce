namespace Pricing.Application.Features.Quotes.Models;

public sealed class QuoteRequest
{
    public Guid SiteId { get; init; }
    public Guid? UserId { get; init; }
    public DateTime? Timestamp { get; init; }
    public string? CouponCode { get; init; }
    public List<QuoteRequestItem> Items { get; init; } = new();
}

public sealed class QuoteRequestItem
{
    public string SkuId { get; init; } = null!;
    public int Qty { get; init; }
    public Guid? BatchId { get; init; }
}
