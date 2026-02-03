using System.Reflection;
using BuildingBlocks.Domain;
using Identity.Application.Abstractions;
using Identity.Domain.Auth;
using Identity.Domain.Audit;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext, IIdentityDbContext
{
    public const string DefaultSchema = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSiteMembership> UserSiteMemberships => Set<UserSiteMembership>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<LoginGuard> LoginGuards => Set<LoginGuard>();
    public DbSet<UserAuditEvent> UserAuditEvents => Set<UserAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ConfigureRowVersion(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureRowVersion(ModelBuilder modelBuilder)
    {
        const string rowVersionPropertyName = nameof(AggregateRoot<Guid>.RowVersion);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null) continue;
            var isAggregateRoot = IsAggregateRoot(entityType.ClrType);
            var prop = entityType.FindProperty(rowVersionPropertyName);
            if (prop is null) continue;

            prop.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
            prop.SetColumnType("rowversion");
            prop.IsConcurrencyToken = isAggregateRoot;
        }
    }

    private static bool IsAggregateRoot(Type? type)
    {
        while (type is not null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                return true;
            type = type.BaseType;
        }
        return false;
    }
}
