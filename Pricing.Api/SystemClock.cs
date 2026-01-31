using Pricing.Application.Abstractions;

namespace Pricing.Api;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
