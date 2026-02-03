using BuildingBlocks.Domain;

namespace Identity.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    public string PhoneNumber { get; private set; } = null!;
    public DateTime? PhoneVerifiedAtUtc { get; private set; }

    public string? FullName { get; private set; }
    public string? NationalId { get; private set; }
    public string? Address { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Landline { get; private set; }
    public DateTime? BirthDateUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public bool IsBanned { get; private set; }
    public DateTime? BannedAtUtc { get; private set; }
    public DateTime? BanUntilUtc { get; private set; }
    public string? BanReason { get; private set; }

    public byte[]? PasswordHash { get; private set; }
    public byte[]? PasswordSalt { get; private set; }
    public int? PasswordIterations { get; private set; }
    public string? PasswordAlgorithm { get; private set; }
    public DateTime? PasswordSetAtUtc { get; private set; }

    private readonly List<UserSiteMembership> _memberships = new();
    public IReadOnlyCollection<UserSiteMembership> Memberships => _memberships;

    private readonly List<UserSession> _sessions = new();
    public IReadOnlyCollection<UserSession> Sessions => _sessions;

    private User() { }

    public static User Create(string normalizedPhoneNumber)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhoneNumber))
            throw new ArgumentException("PhoneNumber required.", nameof(normalizedPhoneNumber));

        return new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = normalizedPhoneNumber.Trim(),
            IsBanned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkPhoneVerified(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        PhoneVerifiedAtUtc ??= nowUtc;
        Touch();
    }

    public void UpdateProfile(
        string? fullName,
        string? nationalId,
        string? address,
        string? postalCode,
        string? landline,
        DateTime? birthDateUtc)
    {
        FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        NationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        Landline = string.IsNullOrWhiteSpace(landline) ? null : landline.Trim();
        if (birthDateUtc.HasValue && birthDateUtc.Value.Kind != DateTimeKind.Utc)
            birthDateUtc = DateTime.SpecifyKind(birthDateUtc.Value, DateTimeKind.Utc);
        BirthDateUtc = birthDateUtc;
        Touch();
    }

    public void MarkLogin(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        LastLoginAtUtc = nowUtc;
        Touch();
    }

    public void SetPassword(byte[] hash, byte[] salt, int iterations, string algorithm, DateTime nowUtc)
    {
        if (hash.Length == 0) throw new ArgumentException("Hash required.", nameof(hash));
        if (salt.Length == 0) throw new ArgumentException("Salt required.", nameof(salt));
        if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));
        if (string.IsNullOrWhiteSpace(algorithm)) throw new ArgumentException("Algorithm required.", nameof(algorithm));
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        PasswordHash = hash;
        PasswordSalt = salt;
        PasswordIterations = iterations;
        PasswordAlgorithm = algorithm.Trim();
        PasswordSetAtUtc = nowUtc;
        Touch();
    }

    public void ClearPassword()
    {
        PasswordHash = null;
        PasswordSalt = null;
        PasswordIterations = null;
        PasswordAlgorithm = null;
        PasswordSetAtUtc = null;
        Touch();
    }

    public bool HasPassword => PasswordHash is not null && PasswordSalt is not null && PasswordIterations.HasValue;

    public void Ban(DateTime nowUtc, string? reason, DateTime? untilUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        if (untilUtc.HasValue && untilUtc.Value.Kind != DateTimeKind.Utc)
            untilUtc = DateTime.SpecifyKind(untilUtc.Value, DateTimeKind.Utc);

        IsBanned = true;
        BannedAtUtc = nowUtc;
        BanUntilUtc = untilUtc;
        BanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Touch();
    }

    public void Unban()
    {
        IsBanned = false;
        BannedAtUtc = null;
        BanUntilUtc = null;
        BanReason = null;
        Touch();
    }

    public bool IsCurrentlyBanned(DateTime nowUtc)
    {
        if (!IsBanned) return false;
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        if (BanUntilUtc.HasValue && BanUntilUtc.Value <= nowUtc) return false;
        return true;
    }

    public bool EnsureMembership(Guid siteId)
    {
        if (siteId == Guid.Empty) throw new ArgumentException("SiteId required.", nameof(siteId));
        if (_memberships.Any(x => x.SiteId == siteId))
            return false;

        _memberships.Add(UserSiteMembership.Create(Id, siteId));
        Touch();
        return true;
    }

    public UserSession AddSession(UserSession session)
    {
        _sessions.Add(session);
        Touch();
        return session;
    }
}
