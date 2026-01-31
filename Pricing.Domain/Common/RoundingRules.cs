namespace Pricing.Domain.Common;

public static class RoundingRules
{
    public static decimal Round(decimal amount, string currency)
    {
        var decimals = string.Equals(currency, "IRR", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        return Math.Round(amount, decimals, MidpointRounding.AwayFromZero);
    }
}
