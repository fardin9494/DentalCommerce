using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Permissions;
using Identity.Application.Models;
using Identity.Domain.Admin;

namespace Identity.Application.Features.Admin.Permissions;

public sealed record UpdatePricingPermissionsCommand(Guid UserId, UpdatePricingPermissionsRequest Request) : IRequest;

public sealed class UpdatePricingPermissionsHandler : IRequestHandler<UpdatePricingPermissionsCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public UpdatePricingPermissionsHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(UpdatePricingPermissionsCommand cmd, CancellationToken ct)
    {
        if (cmd.UserId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(cmd.UserId));

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == cmd.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        var admin = await _db.AdminAccounts.FirstOrDefaultAsync(x => x.UserId == cmd.UserId, ct);
        if (admin is null)
        {
            admin = AdminAccount.Create(cmd.UserId, false, PermissionUiPolicy.Disable, _clock.UtcNow);
            _db.AdminAccounts.Add(admin);
        }

        if (cmd.Request.UiPolicy.HasValue)
            admin.SetUiPolicy(cmd.Request.UiPolicy.Value, _clock.UtcNow);

        if (admin.IsSuperAdmin)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var current = await _db.AdminUserPermissions
            .Where(x => x.UserId == cmd.UserId)
            .ToListAsync(ct);

        foreach (var grant in cmd.Request.Permissions)
        {
            if (!PricingPermissions.IsValid(grant.Key)) continue;
            var exists = current.FirstOrDefault(x => x.PermissionKey == grant.Key);
            if (grant.Granted)
            {
                if (exists is null)
                    _db.AdminUserPermissions.Add(AdminUserPermission.Create(cmd.UserId, grant.Key, _clock.UtcNow));
            }
            else
            {
                if (exists is not null)
                    _db.AdminUserPermissions.Remove(exists);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
