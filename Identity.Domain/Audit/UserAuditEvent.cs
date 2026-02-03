using BuildingBlocks.Domain;

namespace Identity.Domain.Audit;

public sealed class UserAuditEvent : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Detail { get; private set; }
    public string ActorType { get; private set; } = "system";
    public Guid? ActorUserId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private UserAuditEvent() { }

    public static UserAuditEvent Create(
        Guid userId,
        string eventType,
        string title,
        string? detail,
        string actorType,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType required.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required.", nameof(title));
        if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        return new UserAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType.Trim(),
            Title = title.Trim(),
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),
            ActorType = string.IsNullOrWhiteSpace(actorType) ? "system" : actorType.Trim(),
            ActorUserId = actorUserId,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }
}
