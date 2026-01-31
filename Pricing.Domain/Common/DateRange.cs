namespace Pricing.Domain.Common;

public readonly record struct DateRange(DateTime? From, DateTime? To)
{
    public bool Includes(DateTime timestamp)
    {
        if (From.HasValue && timestamp < From.Value) return false;
        if (To.HasValue && timestamp > To.Value) return false;
        return true;
    }

    public static DateRange Always => new(null, null);
}
