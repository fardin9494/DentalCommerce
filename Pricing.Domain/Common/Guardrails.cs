namespace Pricing.Domain.Common;

public sealed class Guardrails
{
    public decimal? MinUnitPrice { get; init; }
    public decimal? MaxDiscountPercent { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
}
