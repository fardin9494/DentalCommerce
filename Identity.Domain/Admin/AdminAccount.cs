using BuildingBlocks.Domain;

namespace Identity.Domain.Admin;

public sealed class AdminAccount : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public bool IsSuperAdmin { get; private set; }
    public PermissionUiPolicy UiPolicy { get; private set; } = PermissionUiPolicy.Disable;

    private AdminAccount() { }

    public static AdminAccount Create(Guid userId, bool isSuperAdmin, PermissionUiPolicy uiPolicy, DateTime nowUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(userId));
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        return new AdminAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsSuperAdmin = isSuperAdmin,
            UiPolicy = uiPolicy,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }

    public void SetUiPolicy(PermissionUiPolicy policy, DateTime nowUtc)
    {
        UiPolicy = policy;
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        UpdatedAt = nowUtc;
    }

    public void PromoteToSuper(DateTime nowUtc)
    {
        IsSuperAdmin = true;
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        UpdatedAt = nowUtc;
    }
}
