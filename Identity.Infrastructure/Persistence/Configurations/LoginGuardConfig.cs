using Identity.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class LoginGuardConfig : IEntityTypeConfiguration<LoginGuard>
{
    public void Configure(EntityTypeBuilder<LoginGuard> b)
    {
        b.ToTable("LoginGuard");
        b.HasKey(x => x.Id);

        b.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.PhoneNumber).IsUnique();

        b.Property(x => x.FailedCount).IsRequired();
        b.Property(x => x.LastFailedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.LockedUntilUtc).HasColumnType("datetime2").IsRequired(false);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();
    }
}
