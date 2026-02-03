namespace Identity.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "DentalCommerce.Identity";
    public string Audience { get; init; } = "DentalCommerce";
    public string SigningKey { get; init; } = null!;
    public int AccessTokenMinutes { get; init; } = 15;
}

