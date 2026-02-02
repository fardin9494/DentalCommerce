using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class DeliverOrderHandler : IRequestHandler<DeliverOrderCommand>
{
    private readonly ISalesDbContext _db;

    public DeliverOrderHandler(ISalesDbContext db) => _db = db;

    public async Task Handle(DeliverOrderCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (order is null) throw new InvalidOperationException("Order not found.");

        await OrderCommandHelpers.SaveWithRetryAsync(
            _db,
            order,
            () =>
            {
                if (order.Status == OrderStatus.Delivered)
                {
                    order.EnsureTimelineEvent("Delivered", cmd.Request.Note, null);
                    return;
                }
                order.MarkDelivered(cmd.Request.Note, null);
            },
            ct);

        await OrderCommandHelpers.EnsureTimelineEventPersistedAsync(
            _db,
            order.Id,
            "Delivered",
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            cmd.Request.Note,
            null,
            order.DeliveredAtUtc,
            ct);
    }
}
