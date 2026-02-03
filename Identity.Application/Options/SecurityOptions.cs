namespace Identity.Application.Options;

public sealed class SecurityOptions
{
    public int MaxFailedAttempts { get; init; } = 5;
    public int LockMinutes { get; init; } = 1440;
    public int ResetWindowMinutes { get; init; } = 30;
}
