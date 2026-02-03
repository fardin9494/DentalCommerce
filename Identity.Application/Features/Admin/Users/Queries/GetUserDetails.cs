using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Models;

namespace Identity.Application.Features.Admin.Users.Queries;

public sealed record GetUserDetailsQuery(Guid UserId) : IRequest<AdminUserDetailsDto>;

public sealed class GetUserDetailsHandler : IRequestHandler<GetUserDetailsQuery, AdminUserDetailsDto>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public GetUserDetailsHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AdminUserDetailsDto> Handle(GetUserDetailsQuery req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(req.UserId));

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        var guard = await _db.LoginGuards.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber == user.PhoneNumber, ct);

        var now = _clock.UtcNow;
        var summary = new AdminUserSummaryDto(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            user.CreatedAt,
            user.LastLoginAtUtc,
            user.IsBanned && (!user.BanUntilUtc.HasValue || user.BanUntilUtc > now),
            user.BanUntilUtc,
            user.BanReason,
            guard?.LockedUntilUtc,
            guard?.FailedCount ?? 0,
            user.HasPassword);

        var sites = await _db.UserSiteMemberships
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SiteMembershipDto(x.SiteId, x.CreatedAt))
            .ToListAsync(ct);

        var sessions = await _db.UserSessions
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminUserSessionDto(
                x.Id,
                x.SiteId,
                x.CreatedAt,
                x.ExpiresAtUtc,
                x.RevokedAtUtc,
                x.UserAgent,
                x.IpAddress))
            .ToListAsync(ct);

        return new AdminUserDetailsDto(summary, sites, sessions);
    }
}
