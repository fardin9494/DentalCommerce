namespace Identity.Application.Models;

public sealed record AdminUserSummaryDto(
    Guid UserId,
    string PhoneNumber,
    string? FullName,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    bool IsBanned,
    DateTime? BanUntilUtc,
    string? BanReason,
    DateTime? LockedUntilUtc,
    int FailedCount,
    bool HasPassword);

public sealed record AdminUserSessionDto(
    Guid SessionId,
    Guid SiteId,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    string? UserAgent,
    string? IpAddress);

public sealed record AdminUserDetailsDto(
    AdminUserSummaryDto User,
    IReadOnlyList<SiteMembershipDto> Sites,
    IReadOnlyList<AdminUserSessionDto> Sessions);

public sealed record AdminUsersPageDto(
    int TotalCount,
    IReadOnlyList<AdminUserSummaryDto> Items);
