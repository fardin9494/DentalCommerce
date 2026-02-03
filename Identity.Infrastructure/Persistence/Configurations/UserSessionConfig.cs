using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserSessionConfig : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> b)
    {
        b.ToTable("UserSession");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.SiteId).IsRequired();

        b.Property(x => x.RefreshTokenHash).HasColumnType("varbinary(32)").IsRequired();
        b.HasIndex(x => x.RefreshTokenHash).IsUnique();

        b.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.RevokedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.ReplacedBySessionId).IsRequired(false);

        b.Property(x => x.UserAgent).HasMaxLength(512).IsRequired(false);
        b.Property(x => x.IpAddress).HasMaxLength(64).IsRequired(false);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();

        b.HasIndex(x => new { x.UserId, x.SiteId });
        b.HasIndex(x => x.ExpiresAtUtc);

        b.HasOne<User>()
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

