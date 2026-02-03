namespace Identity.Application.Models;

public sealed record SiteMembershipDto(
    Guid SiteId,
    DateTime JoinedAtUtc);

