namespace Pricing.Domain.Quotes;

public sealed class PriceQuote
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SiteId { get; init; }
    public Guid? UserId { get; init; }
    public string Currency { get; init; } = "IRR";
    public DateTime Timestamp { get; init; }

    public List<QuoteLine> Lines { get; init; } = new();

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal FinalTotal { get; set; }
    public decimal CashbackTotal { get; set; }

    public List<QuoteSourceResult> AppliedSources { get; init; } = new();
    public List<QuoteSourceResult> RejectedSources { get; init; } = new();
    public List<QuoteTraceEntry> Trace { get; init; } = new();
}
