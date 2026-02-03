using BuildingBlocks.Domain;

namespace Identity.Domain.Users;

public sealed class UserSiteMembership : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid SiteId { get; private set; }

    private UserSiteMembership() { }

    internal static UserSiteMembership Create(Guid userId, Guid siteId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(userId));
        if (siteId == Guid.Empty) throw new ArgumentException("SiteId required.", nameof(siteId));

        return new UserSiteMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SiteId = siteId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

