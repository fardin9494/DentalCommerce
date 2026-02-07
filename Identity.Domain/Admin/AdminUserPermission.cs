using BuildingBlocks.Domain;

namespace Identity.Domain.Admin;

public sealed class AdminUserPermission : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string PermissionKey { get; private set; } = null!;

    private AdminUserPermission() { }

    public static AdminUserPermission Create(Guid userId, string permissionKey, DateTime nowUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(permissionKey)) throw new ArgumentException("PermissionKey required.", nameof(permissionKey));
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        return new AdminUserPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionKey = permissionKey.Trim(),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }
}
