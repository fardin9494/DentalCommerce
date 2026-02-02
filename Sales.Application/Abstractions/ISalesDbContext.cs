using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sales.Domain.Orders;

namespace Sales.Application.Abstractions;

public interface ISalesDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }
    DbSet<OrderTimelineEntry> OrderTimeline { get; }
    DbSet<OrderNote> OrderNotes { get; }
    DbSet<OrderRefund> OrderRefunds { get; }
    DbSet<OrderRefundLine> OrderRefundLines { get; }

    ChangeTracker ChangeTracker { get; }
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
