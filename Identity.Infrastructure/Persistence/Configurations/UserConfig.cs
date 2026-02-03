using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("User");
        b.HasKey(x => x.Id);

        b.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.PhoneNumber).IsUnique();

        b.Property(x => x.FullName).HasMaxLength(200).IsRequired(false);
        b.Property(x => x.NationalId).HasMaxLength(20).IsRequired(false);
        b.Property(x => x.Address).HasMaxLength(1000).IsRequired(false);
        b.Property(x => x.PostalCode).HasMaxLength(32).IsRequired(false);
        b.Property(x => x.Landline).HasMaxLength(32).IsRequired(false);
        b.Property(x => x.BirthDateUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.PhoneVerifiedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.LastLoginAtUtc).HasColumnType("datetime2").IsRequired(false);

        b.Property(x => x.IsBanned).IsRequired();
        b.Property(x => x.BannedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.BanUntilUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.BanReason).HasMaxLength(512).IsRequired(false);

        b.Property(x => x.PasswordHash).HasColumnType("varbinary(64)").IsRequired(false);
        b.Property(x => x.PasswordSalt).HasColumnType("varbinary(32)").IsRequired(false);
        b.Property(x => x.PasswordIterations).IsRequired(false);
        b.Property(x => x.PasswordAlgorithm).HasMaxLength(64).IsRequired(false);
        b.Property(x => x.PasswordSetAtUtc).HasColumnType("datetime2").IsRequired(false);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();

        b.Metadata.FindNavigation(nameof(User.Memberships))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(User.Sessions))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
