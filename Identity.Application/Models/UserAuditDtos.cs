namespace Identity.Application.Models;

public sealed record UserAuditItemDto(
    Guid Id,
    string EventType,
    string Title,
    string? Detail,
    string ActorType,
    Guid? ActorUserId,
    string? IpAddress,
    string? UserAgent,
    DateTime OccurredAtUtc);

public sealed record UserAuditListDto(
    int Total,
    IReadOnlyList<UserAuditItemDto> Items);
