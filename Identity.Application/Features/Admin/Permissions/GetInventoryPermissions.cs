using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Permissions;
using Identity.Application.Options;
using Identity.Application.Models;
using Identity.Domain.Admin;
using Identity.Domain.Users;

namespace Identity.Application.Features.Admin.Permissions;

public sealed record GetInventoryPermissionsQuery(Guid UserId) : IRequest<InventoryPermissionDto>;

public sealed class GetInventoryPermissionsHandler : IRequestHandler<GetInventoryPermissionsQuery, InventoryPermissionDto>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;
    private readonly AdminOptions _adminOptions;

    public GetInventoryPermissionsHandler(IIdentityDbContext db, IClock clock, AdminOptions adminOptions)
    {
        _db = db;
        _clock = clock;
        _adminOptions = adminOptions;
    }

    public async Task<InventoryPermissionDto> Handle(GetInventoryPermissionsQuery req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(req.UserId));

        var admin = await _db.AdminAccounts.FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        var isSuper = admin?.IsSuperAdmin ?? false;

        if (!isSuper && !string.IsNullOrWhiteSpace(_adminOptions.SuperAdminPhone))
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.UserId, ct);
            if (user is not null)
            {
                var normalized = PhoneNumber.Normalize(_adminOptions.SuperAdminPhone);
                if (string.Equals(user.PhoneNumber, normalized, StringComparison.Ordinal))
                {
                    var now = _clock.UtcNow;
                    if (admin is null)
                    {
                        admin = AdminAccount.Create(req.UserId, true, PermissionUiPolicy.Disable, now);
                        _db.AdminAccounts.Add(admin);
                    }
                    else
                    {
                        admin.PromoteToSuper(now);
                    }

                    await _db.SaveChangesAsync(ct);
                    isSuper = true;
                }
            }
        }
        var uiPolicy = admin?.UiPolicy ?? PermissionUiPolicy.Disable;

        var granted = await _db.AdminUserPermissions.AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .Select(x => x.PermissionKey)
            .ToListAsync(ct);

        var grantedSet = granted.ToHashSet();

        var items = InventoryPermissions.All
            .Select(p => new InventoryPermissionItemDto(
                p.Key,
                p.Title,
                p.Description,
                isSuper || grantedSet.Contains(p.Key)))
            .ToList();

        return new InventoryPermissionDto(req.UserId, isSuper, uiPolicy, items);
    }
}
