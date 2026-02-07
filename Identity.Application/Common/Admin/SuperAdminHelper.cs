using Identity.Application.Abstractions;
using Identity.Application.Options;
using Identity.Domain.Admin;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Common.Admin;

public static class SuperAdminHelper
{
    public static async Task EnsureSuperAdminAsync(
        IIdentityDbContext db,
        AdminOptions options,
        User user,
        DateTime now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.SuperAdminPhone)) return;

        var normalized = PhoneNumber.Normalize(options.SuperAdminPhone);
        if (!string.Equals(user.PhoneNumber, normalized, StringComparison.Ordinal)) return;

        var admin = await db.AdminAccounts.FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (admin is null)
        {
            admin = AdminAccount.Create(user.Id, true, PermissionUiPolicy.Disable, now);
            db.AdminAccounts.Add(admin);
        }
        else if (!admin.IsSuperAdmin)
        {
            admin.PromoteToSuper(now);
        }
    }
}
