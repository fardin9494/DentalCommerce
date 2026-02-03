namespace Identity.Application.Options;

public sealed class SessionOptions
{
    public int RefreshTokenTtlDays { get; init; } = 30;
}

