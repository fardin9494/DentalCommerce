namespace Pricing.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
