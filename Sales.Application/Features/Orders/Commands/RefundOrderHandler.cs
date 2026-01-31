using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class RefundOrderHandler : IRequestHandler<RefundOrderCommand>
{
    private readonly ISalesDbContext _db;

    public RefundOrderHandler(ISalesDbContext db) => _db = db;

    public async Task Handle(RefundOrderCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (order is null) throw new InvalidOperationException("Order not found.");

        var data = new
        {
            reason = cmd.Request.Reason,
            amount = cmd.Request.Amount
        };
        var dataJson = JsonSerializer.Serialize(data);

        await OrderCommandHelpers.SaveWithRetryAsync(
            _db,
            order,
            () =>
            {
                if (order.Status == OrderStatus.Refunded) return;
                order.MarkRefunded(cmd.Request.Note, dataJson);
            },
            ct);
    }
}
