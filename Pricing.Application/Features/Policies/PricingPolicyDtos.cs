using Pricing.Domain.Common;

namespace Pricing.Application.Features.Policies;

public sealed class PricingPolicyDto
{
    public Guid Id { get; init; }
    public Guid? SiteId { get; init; }
    public StackingMode DefaultStackingMode { get; init; }
}
