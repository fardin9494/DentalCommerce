using BuildingBlocks.Domain;

namespace Identity.Domain.Users;

public sealed class UserSession : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid SiteId { get; private set; }

    public byte[] RefreshTokenHash { get; private set; } = Array.Empty<byte>();

    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }

    public string? UserAgent { get; private set; }
    public string? IpAddress { get; private set; }

    private UserSession() { }

    public static UserSession Create(
        Guid userId,
        Guid siteId,
        byte[] refreshTokenHash,
        DateTime expiresAtUtc,
        string? userAgent,
        string? ipAddress)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(userId));
        if (siteId == Guid.Empty) throw new ArgumentException("SiteId required.", nameof(siteId));
        if (refreshTokenHash.Length == 0) throw new ArgumentException("RefreshTokenHash required.", nameof(refreshTokenHash));
        if (expiresAtUtc.Kind != DateTimeKind.Utc)
            expiresAtUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);

        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SiteId = siteId,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAtUtc = expiresAtUtc,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool IsActive(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        return RevokedAtUtc is null && nowUtc < ExpiresAtUtc;
    }

    public void Revoke(DateTime nowUtc, Guid? replacedBySessionId = null)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        RevokedAtUtc = nowUtc;
        ReplacedBySessionId = replacedBySessionId;
        Touch();
    }

    public void Rotate(byte[] newRefreshTokenHash, DateTime nowUtc, Guid newSessionId)
    {
        if (newRefreshTokenHash.Length == 0) throw new ArgumentException("RefreshTokenHash required.", nameof(newRefreshTokenHash));
        Revoke(nowUtc, newSessionId);
    }
}

