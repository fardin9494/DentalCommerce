namespace Pricing.Domain.Common;

public sealed class StackingPolicy
{
    public const StackingMode DefaultMode = StackingMode.BestOfEachGroup;

    public StackingMode Mode { get; init; } = DefaultMode;
    public string? DefaultGroup { get; init; }
}
