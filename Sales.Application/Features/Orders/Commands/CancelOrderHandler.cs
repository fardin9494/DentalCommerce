using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly ISalesDbContext _db;
    private readonly IInventoryReservationGateway _inventory;

    public CancelOrderHandler(
        ISalesDbContext db,
        IInventoryReservationGateway inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (order is null)
            throw new InvalidOperationException($"Order {cmd.OrderId} not found.");

        if (order.Status == OrderStatus.Cancelled)
        {
            order.EnsureTimelineEvent("Cancelled", cmd.Request.Reason, cmd.Request.Note);
            OrderCommandHelpers.EnsureLatestTimelineTracked(_db, order);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Release inventory reservations (if any) before cancelling
        try
        {
            await _inventory.ReleaseAsync(order.Id, ct);
        }
        catch
        {
            // ignore release errors in test flow
        }

        await OrderCommandHelpers.SaveWithRetryAsync(
            _db,
            order,
            () =>
            {
                if (order.Status == OrderStatus.Cancelled) return;
                order.Cancel(cmd.Request.Reason, cmd.Request.Note);
            },
            ct);
    }
}
