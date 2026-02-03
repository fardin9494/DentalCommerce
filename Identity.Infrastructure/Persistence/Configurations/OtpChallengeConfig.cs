using Identity.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class OtpChallengeConfig : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> b)
    {
        b.ToTable("OtpChallenge");
        b.HasKey(x => x.Id);

        b.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
        b.Property(x => x.SiteId).IsRequired();
        b.Property(x => x.Purpose).IsRequired();

        b.Property(x => x.CodeHash).HasColumnType("varbinary(32)").IsRequired();
        b.Property(x => x.Salt).HasColumnType("varbinary(32)").IsRequired();

        b.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.AttemptCount).IsRequired();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();

        b.HasIndex(x => new { x.PhoneNumber, x.SiteId, x.CreatedAt });
        b.HasIndex(x => x.ExpiresAtUtc);
    }
}

