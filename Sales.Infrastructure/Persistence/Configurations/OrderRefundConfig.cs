using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence.Configurations;

public sealed class OrderRefundConfig : IEntityTypeConfiguration<OrderRefund>
{
    public void Configure(EntityTypeBuilder<OrderRefund> b)
    {
        b.ToTable("OrderRefund");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderId).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.Destination).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Reason).HasMaxLength(512).IsRequired(false);
        b.Property(x => x.Note).HasMaxLength(2000).IsRequired(false);
        b.Property(x => x.RequestedBy).HasMaxLength(256).IsRequired(false);
        b.Property(x => x.RequestedAtUtc).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.ApprovedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.RejectedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2").IsRequired(false);
        b.Property(x => x.RejectionReason).HasMaxLength(512).IsRequired(false);
        b.Property(x => x.WalletReference).HasMaxLength(256).IsRequired(false);

        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.Status);

        b.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.RefundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
