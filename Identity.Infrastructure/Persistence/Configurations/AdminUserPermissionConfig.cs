using Identity.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class AdminUserPermissionConfig : IEntityTypeConfiguration<AdminUserPermission>
{
    public void Configure(EntityTypeBuilder<AdminUserPermission> b)
    {
        b.ToTable("AdminUserPermission");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.PermissionKey).HasMaxLength(128).IsRequired();

        b.HasIndex(x => new { x.UserId, x.PermissionKey }).IsUnique();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();
    }
}
