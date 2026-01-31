using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;

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

        order.MarkDelivered(cmd.Request.Note, null);
        await _db.SaveChangesAsync(ct);
    }
}
