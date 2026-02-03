using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signing;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(JwtOptions options)
    {
        _options = options;

        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 characters.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signing = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(Math.Max(1, _options.AccessTokenMinutes));

    public string CreateAccessToken(Guid userId, Guid siteId, Guid sessionId, string phoneNumber, DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        var expires = nowUtc.Add(AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("siteId", siteId.ToString()),
            new("sid", sessionId.ToString()),
            new("phone", phoneNumber)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expires,
            signingCredentials: _signing);

        return _handler.WriteToken(token);
    }
}

