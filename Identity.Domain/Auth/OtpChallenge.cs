using BuildingBlocks.Domain;

namespace Identity.Domain.Auth;

public sealed class OtpChallenge : BaseEntity<Guid>
{
    public string PhoneNumber { get; private set; } = null!;
    public Guid SiteId { get; private set; }
    public OtpPurpose Purpose { get; private set; }

    public byte[] CodeHash { get; private set; } = Array.Empty<byte>();
    public byte[] Salt { get; private set; } = Array.Empty<byte>();
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }

    private OtpChallenge() { }

    public static OtpChallenge Create(
        string phoneNumber,
        Guid siteId,
        OtpPurpose purpose,
        byte[] codeHash,
        byte[] salt,
        DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("PhoneNumber required.", nameof(phoneNumber));
        if (siteId == Guid.Empty) throw new ArgumentException("SiteId required.", nameof(siteId));
        if (codeHash.Length == 0) throw new ArgumentException("CodeHash required.", nameof(codeHash));
        if (salt.Length == 0) throw new ArgumentException("Salt required.", nameof(salt));
        if (expiresAtUtc.Kind != DateTimeKind.Utc)
            expiresAtUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);

        return new OtpChallenge
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber.Trim(),
            SiteId = siteId,
            Purpose = purpose,
            CodeHash = codeHash,
            Salt = salt,
            ExpiresAtUtc = expiresAtUtc,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        return nowUtc >= ExpiresAtUtc;
    }

    public bool IsVerified => VerifiedAtUtc.HasValue;

    public void MarkFailedAttempt()
    {
        AttemptCount++;
        Touch();
    }

    public void MarkVerified(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        VerifiedAtUtc = nowUtc;
        Touch();
    }
}

