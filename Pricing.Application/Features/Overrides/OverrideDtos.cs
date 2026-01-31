using Pricing.Domain.Overrides;

namespace Pricing.Application.Features.Overrides;

public sealed class PriceOverrideDto
{
    public Guid Id { get; init; }
    public OverrideScope ScopeType { get; init; }
    public Guid ScopeId { get; init; }
    public string SkuId { get; init; } = string.Empty;
    public OverrideType OverrideType { get; init; }
    public decimal Value { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public int Priority { get; init; }
    public string? StackingGroup { get; init; }
}
