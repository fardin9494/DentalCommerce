namespace Pricing.Domain.Common;

public enum StackingMode
{
    Cascading = 0,
    BestPrice = 1,
    PriorityOnly = 2,
    BestOfEachGroup = 3
}
