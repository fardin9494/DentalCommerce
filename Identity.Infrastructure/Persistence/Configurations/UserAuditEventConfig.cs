using Identity.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserAuditEventConfig : IEntityTypeConfiguration<UserAuditEvent>
{
    public void Configure(EntityTypeBuilder<UserAuditEvent> b)
    {
        b.ToTable("UserAuditEvent");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();
        b.HasIndex(x => x.UserId);

        b.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Detail).HasMaxLength(1000).IsRequired(false);
        b.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
        b.Property(x => x.ActorUserId).IsRequired(false);
        b.Property(x => x.IpAddress).HasMaxLength(64).IsRequired(false);
        b.Property(x => x.UserAgent).HasMaxLength(512).IsRequired(false);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();
    }
}
