using Pricing.Domain.Quotes;

namespace Pricing.Application.Features.Quotes.Models;

public sealed class QuoteResponse
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public Guid? UserId { get; init; }
    public string Currency { get; init; } = "IRR";
    public DateTime Timestamp { get; init; }

    public List<QuoteLineResponse> Lines { get; init; } = new();
    public decimal Subtotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal FinalTotal { get; init; }
    public decimal CashbackTotal { get; init; }

    public List<QuoteSourceResult> AppliedSources { get; init; } = new();
    public List<QuoteSourceResult> RejectedSources { get; init; } = new();
    public List<QuoteTraceEntry> Trace { get; init; } = new();
}

public sealed class QuoteLineResponse
{
    public string SkuId { get; init; } = null!;
    public Guid? BatchId { get; init; }
    public int Quantity { get; init; }
    public decimal BaseUnitPrice { get; init; }
    public decimal FinalUnitPrice { get; init; }
    public bool IsGift { get; init; }
    public List<PriceAdjustment> Adjustments { get; init; } = new();
}
