using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Audit;
using Identity.Domain.Audit;

namespace Identity.Application.Features.Admin.Users.Commands;

public sealed record RevokeUserSessionsCommand(Guid UserId) : IRequest;

public sealed class RevokeUserSessionsHandler : IRequestHandler<RevokeUserSessionsCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public RevokeUserSessionsHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(RevokeUserSessionsCommand cmd, CancellationToken ct)
    {
        var sessions = await _db.UserSessions
            .Where(x => x.UserId == cmd.UserId && x.RevokedAtUtc == null)
            .ToListAsync(ct);

        if (sessions.Count == 0) return;

        var now = _clock.UtcNow;
        foreach (var s in sessions)
            s.Revoke(now);

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            cmd.UserId,
            AuditEventTypes.SessionsRevoked,
            "ابطال همه نشست‌ها",
            "همه نشست‌های فعال کاربر باطل شد.",
            "admin",
            null,
            null,
            null,
            now));

        await _db.SaveChangesAsync(ct);
    }
}
