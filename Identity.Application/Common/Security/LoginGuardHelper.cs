using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Options;
using Identity.Domain.Auth;

namespace Identity.Application.Common.Security;

public static class LoginGuardHelper
{
    public static async Task EnsureNotLockedAsync(
        IIdentityDbContext db,
        string phoneNumber,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var guard = await db.LoginGuards.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, ct);
        if (guard is null) return;

        if (guard.IsLocked(nowUtc))
        {
            var until = guard.LockedUntilUtc?.ToLocalTime().ToString("yyyy/MM/dd HH:mm") ?? "نامشخص";
            throw new InvalidOperationException($"این شماره به‌دلیل تلاش‌های ناموفق تا {until} قفل شده است. علت: تلاش‌های ناموفق.");
        }

        if (guard.LockedUntilUtc.HasValue && guard.LockedUntilUtc.Value <= nowUtc)
        {
            guard.Reset();
            await db.SaveChangesAsync(ct);
        }
    }

    public static async Task RegisterFailureAsync(
        IIdentityDbContext db,
        SecurityOptions options,
        string phoneNumber,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var guard = await db.LoginGuards.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, ct);
        if (guard is null)
        {
            guard = LoginGuard.Create(phoneNumber, nowUtc);
            db.LoginGuards.Add(guard);
        }

        guard.RegisterFailure(nowUtc, options.MaxFailedAttempts, options.LockMinutes, options.ResetWindowMinutes);
        await db.SaveChangesAsync(ct);
    }

    public static async Task ResetAsync(
        IIdentityDbContext db,
        string phoneNumber,
        CancellationToken ct)
    {
        var guard = await db.LoginGuards.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, ct);
        if (guard is null) return;
        guard.Reset();
        await db.SaveChangesAsync(ct);
    }
}
