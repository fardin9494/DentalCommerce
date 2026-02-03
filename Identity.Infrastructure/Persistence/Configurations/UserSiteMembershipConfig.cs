using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserSiteMembershipConfig : IEntityTypeConfiguration<UserSiteMembership>
{
    public void Configure(EntityTypeBuilder<UserSiteMembership> b)
    {
        b.ToTable("UserSiteMembership");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.SiteId).IsRequired();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();

        b.HasIndex(x => new { x.UserId, x.SiteId }).IsUnique();

        b.HasOne<User>()
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

