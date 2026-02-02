using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class ReturnOrderHandler : IRequestHandler<ReturnOrderCommand>
{
    private readonly ISalesDbContext _db;

    public ReturnOrderHandler(ISalesDbContext db) => _db = db;

    public async Task Handle(ReturnOrderCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (order is null) throw new InvalidOperationException("Order not found.");

        var data = new { reason = cmd.Request.Reason };
        var dataJson = JsonSerializer.Serialize(data);

        await OrderCommandHelpers.SaveWithRetryAsync(
            _db,
            order,
            () =>
            {
                if (order.Status == OrderStatus.Returned)
                {
                    order.EnsureTimelineEvent("Returned", cmd.Request.Note, dataJson);
                    return;
                }
                order.MarkReturned(cmd.Request.Note, dataJson);
            },
            ct);
    }
}
