using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Audit;
using Identity.Application.Common.Security;
using Identity.Domain.Audit;

namespace Identity.Application.Features.Admin.Users.Commands;

public sealed record UnlockUserCommand(Guid UserId) : IRequest;

public sealed class UnlockUserHandler : IRequestHandler<UnlockUserCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public UnlockUserHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(UnlockUserCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cmd.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        await LoginGuardHelper.ResetAsync(_db, user.PhoneNumber, ct);

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            user.Id,
            AuditEventTypes.UserUnlocked,
            "رفع قفل تلاش‌های ناموفق",
            "قفل تلاش‌های ورود پاک شد.",
            "admin",
            null,
            null,
            null,
            _clock.UtcNow));

        await _db.SaveChangesAsync(ct);
    }
}
