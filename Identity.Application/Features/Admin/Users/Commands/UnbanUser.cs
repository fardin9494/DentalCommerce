using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Audit;
using Identity.Domain.Audit;

namespace Identity.Application.Features.Admin.Users.Commands;

public sealed record UnbanUserCommand(Guid UserId) : IRequest;

public sealed class UnbanUserHandler : IRequestHandler<UnbanUserCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public UnbanUserHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(UnbanUserCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == cmd.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        user.Unban();

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            user.Id,
            AuditEventTypes.UserUnbanned,
            "آن‌بن کاربر",
            "بن کاربر برداشته شد.",
            "admin",
            null,
            null,
            null,
            _clock.UtcNow));

        await _db.SaveChangesAsync(ct);
    }
}
