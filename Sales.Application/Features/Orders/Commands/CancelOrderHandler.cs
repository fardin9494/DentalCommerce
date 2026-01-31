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

        // Check if order can be cancelled (domain method will throw if not)
        // But we need to release inventory first if order is Placed
        var wasPlaced = order.Status == OrderStatus.Placed;

        // Release inventory reservation if order was placed
        if (wasPlaced)
        {
            try
            {
                await _inventory.ReleaseAsync(order.Id, ct);
            }
            catch (Exception ex)
            {
                // Log but don't fail - inventory might already be released or order might not have reservation
                // In production, you might want to log this properly
            }
        }

        // Cancel the order (domain method validates status)
        order.Cancel(cmd.Request.Reason, cmd.Request.Note);
        await _db.SaveChangesAsync(ct);
    }
}
