namespace Identity.Application.Options;

public sealed class PasswordOptions
{
    public int MinLength { get; init; } = 8;
    public int Pbkdf2Iterations { get; init; } = 210_000;
}

