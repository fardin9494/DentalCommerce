using BuildingBlocks.Domain;

namespace Identity.Domain.Auth;

public sealed class LoginGuard : BaseEntity<Guid>
{
    public string PhoneNumber { get; private set; } = null!;
    public int FailedCount { get; private set; }
    public DateTime? LastFailedAtUtc { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }

    private LoginGuard() { }

    public static LoginGuard Create(string phoneNumber, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("PhoneNumber required.", nameof(phoneNumber));
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        return new LoginGuard
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber.Trim(),
            FailedCount = 0,
            LastFailedAtUtc = null,
            LockedUntilUtc = null,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }

    public bool IsLocked(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        return LockedUntilUtc.HasValue && LockedUntilUtc.Value > nowUtc;
    }

    public void ClearIfExpired(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        if (LockedUntilUtc.HasValue && LockedUntilUtc.Value <= nowUtc)
            Reset();
    }

    public void RegisterFailure(DateTime nowUtc, int maxAttempts, int lockMinutes, int resetWindowMinutes)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        if (maxAttempts <= 0) maxAttempts = 1;

        if (LastFailedAtUtc.HasValue)
        {
            var resetAfter = nowUtc.AddMinutes(-Math.Max(1, resetWindowMinutes));
            if (LastFailedAtUtc.Value < resetAfter)
            {
                FailedCount = 0;
            }
        }

        FailedCount++;
        LastFailedAtUtc = nowUtc;

        if (FailedCount >= maxAttempts)
            LockedUntilUtc = nowUtc.AddMinutes(Math.Max(1, lockMinutes));

        Touch();
    }

    public void Reset()
    {
        FailedCount = 0;
        LastFailedAtUtc = null;
        LockedUntilUtc = null;
        Touch();
    }
}
