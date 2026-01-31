using BuildingBlocks.Domain;
using Pricing.Domain.Common;

namespace Pricing.Domain.Policies;

public sealed class PricingPolicy : AggregateRoot<Guid>
{
    public Guid? SiteId { get; private set; }
    public StackingMode DefaultStackingMode { get; private set; } = StackingPolicy.DefaultMode;

    private PricingPolicy() { }

    public static PricingPolicy Create(Guid? siteId, StackingMode stackingMode)
    {
        return new PricingPolicy
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            DefaultStackingMode = stackingMode
        };
    }

    public void Update(StackingMode stackingMode)
    {
        DefaultStackingMode = stackingMode;
        Touch();
    }
}
