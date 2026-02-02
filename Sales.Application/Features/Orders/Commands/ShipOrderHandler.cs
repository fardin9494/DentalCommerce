using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class ShipOrderHandler : IRequestHandler<ShipOrderCommand>
{
    private readonly ISalesDbContext _db;

    public ShipOrderHandler(ISalesDbContext db) => _db = db;

    public async Task Handle(ShipOrderCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (order is null) throw new InvalidOperationException("Order not found.");

        var data = new
        {
            carrier = cmd.Request.Carrier,
            trackingCode = cmd.Request.TrackingCode
        };
        var dataJson = JsonSerializer.Serialize(data);

        await OrderCommandHelpers.SaveWithRetryAsync(
            _db,
            order,
            () =>
            {
                if (order.Status == OrderStatus.Shipped)
                {
                    order.EnsureTimelineEvent("Shipped", cmd.Request.Note, dataJson);
                    return;
                }
                order.MarkShipped(cmd.Request.Note, dataJson);
            },
            ct);
    }
}
