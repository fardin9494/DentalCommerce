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

        var updatedAt = order.UpdatedAt;
        var status = order.Status;
        var timelineEntries = db.ChangeTracker.Entries<OrderTimelineEntry>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        var affected = await db.Orders
            .Where(o => o.Id == order.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, status)
                .SetProperty(o => o.UpdatedAt, updatedAt),
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

    private static void DetachAddedTimeline(ISalesDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries<OrderTimelineEntry>()
                     .Where(e => e.State == EntityState.Added)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
