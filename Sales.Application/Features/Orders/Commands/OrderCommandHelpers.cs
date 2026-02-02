using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

internal static class OrderCommandHelpers
{
    public static async Task SaveWithRetryAsync(
        ISalesDbContext db,
        Order order,
        Action apply,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            apply();
            EnsureLatestTimelineTracked(db, order);
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                DetachAddedTimeline(db);
                await db.Entry(order).ReloadAsync(ct);
            }
        }

        apply();
        EnsureLatestTimelineTracked(db, order);

        var updatedAt = order.UpdatedAt;
        var status = order.Status;
        var cancelledAt = order.CancelledAtUtc;
        var shippedAt = order.ShippedAtUtc;
        var deliveredAt = order.DeliveredAtUtc;
        var returnedAt = order.ReturnedAtUtc;
        var refundedAt = order.RefundedAtUtc;
        var timelineEntries = db.ChangeTracker.Entries<OrderTimelineEntry>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        var affected = await db.Orders
            .Where(o => o.Id == order.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, status)
                .SetProperty(o => o.UpdatedAt, updatedAt)
                .SetProperty(o => o.CancelledAtUtc, cancelledAt)
                .SetProperty(o => o.ShippedAtUtc, shippedAt)
                .SetProperty(o => o.DeliveredAtUtc, deliveredAt)
                .SetProperty(o => o.ReturnedAtUtc, returnedAt)
                .SetProperty(o => o.RefundedAtUtc, refundedAt),
                ct);

        if (affected == 0)
            throw new InvalidOperationException("Order not found.");

        db.ChangeTracker.Clear();
        if (timelineEntries.Count > 0)
        {
            db.OrderTimeline.AddRange(timelineEntries);
            await db.SaveChangesAsync(ct);
        }
    }

    public static void EnsureLatestTimelineTracked(ISalesDbContext db, Order order)
    {
        if (db.ChangeTracker.Entries<OrderTimelineEntry>().Any(e => e.State == EntityState.Added))
            return;

        var latest = order.Timeline
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (latest is null)
            return;

        var alreadyTracked = db.ChangeTracker.Entries<OrderTimelineEntry>()
            .Any(e => e.Entity.Id == latest.Id);

        if (!alreadyTracked)
            db.OrderTimeline.Add(latest);
    }

    private static void DetachAddedTimeline(ISalesDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries<OrderTimelineEntry>()
                     .Where(e => e.State == EntityState.Added)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    public static async Task EnsureTimelineEventPersistedAsync(
        ISalesDbContext db,
        Guid orderId,
        string eventType,
        OrderStatus? fromStatus,
        OrderStatus? toStatus,
        string? message,
        string? dataJson,
        DateTime? whenUtc,
        CancellationToken ct)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType required.", nameof(eventType));

        var exists = await db.OrderTimeline
            .AsNoTracking()
            .AnyAsync(t => t.OrderId == orderId && t.EventType == eventType, ct);

        if (exists) return;

        db.OrderTimeline.Add(OrderTimelineEntry.CreateExternal(
            orderId,
            eventType,
            fromStatus,
            toStatus,
            message,
            dataJson,
            whenUtc));

        await db.SaveChangesAsync(ct);
    }
}
