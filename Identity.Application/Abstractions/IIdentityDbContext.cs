using Identity.Domain.Admin;
using Identity.Domain.Auth;
using Identity.Domain.Audit;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Identity.Application.Abstractions;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSiteMembership> UserSiteMemberships { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<OtpChallenge> OtpChallenges { get; }
    DbSet<LoginGuard> LoginGuards { get; }
    DbSet<UserAuditEvent> UserAuditEvents { get; }
    DbSet<AdminAccount> AdminAccounts { get; }
    DbSet<AdminUserPermission> AdminUserPermissions { get; }

    ChangeTracker ChangeTracker { get; }
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
