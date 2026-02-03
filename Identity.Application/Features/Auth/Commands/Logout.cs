using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;

namespace Identity.Application.Features.Auth.Commands;

public sealed record LogoutCommand(Guid SessionId) : IRequest;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public LogoutHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        if (cmd.SessionId == Guid.Empty) return;

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct);

        if (session is null) return;

        session.Revoke(_clock.UtcNow);
        await _db.SaveChangesAsync(ct);
    }
}

