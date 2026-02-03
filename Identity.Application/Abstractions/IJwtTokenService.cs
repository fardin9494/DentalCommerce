namespace Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateAccessToken(Guid userId, Guid siteId, Guid sessionId, string phoneNumber, DateTime nowUtc);
    TimeSpan AccessTokenLifetime { get; }
}

