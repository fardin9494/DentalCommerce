namespace Identity.Application.Options;

public sealed class OtpOptions
{
    public int CodeLength { get; init; } = 6;
    public int TtlSeconds { get; init; } = 180;
    public int SendCooldownSeconds { get; init; } = 60;
    public int MaxAttempts { get; init; } = 5;
}

