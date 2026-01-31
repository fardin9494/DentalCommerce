namespace Pricing.Domain.Quotes;

public sealed class QuoteLine
{
    public string SkuId { get; init; } = null!;
    public Guid? BatchId { get; init; }
    public int Quantity { get; set; }
    public decimal BaseUnitPrice { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public bool IsGift { get; set; }
    public List<PriceAdjustment> Adjustments { get; } = new();
}
