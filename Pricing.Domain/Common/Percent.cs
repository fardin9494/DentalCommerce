namespace Pricing.Domain.Common;

public readonly record struct Percent(decimal Value)
{
    public decimal AsFraction => Value / 100m;
    public decimal Apply(decimal amount) => amount * Value / 100m;

    public static Percent FromFraction(decimal fraction) => new(fraction * 100m);
}
