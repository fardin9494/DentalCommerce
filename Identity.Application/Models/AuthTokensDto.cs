namespace Identity.Application.Models;

public sealed record AuthTokensDto(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    Guid UserId,
    Guid SiteId);

