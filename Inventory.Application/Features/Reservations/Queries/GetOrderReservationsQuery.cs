using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.Queries;

public sealed record GetOrderReservationsQuery(Guid OrderId) : IRequest<IReadOnlyList<OrderReservationDto>>;

public sealed record OrderReservationDto(
    Guid Id,
    Guid OrderId,
    Guid StockItemId,
    string SkuId,
    decimal Qty,
    DateTime CreatedAt);

public sealed class GetOrderReservationsHandler : IRequestHandler<GetOrderReservationsQuery, IReadOnlyList<OrderReservationDto>>
{
    private readonly IInventoryDbContext _db;

    public GetOrderReservationsHandler(IInventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderReservationDto>> Handle(GetOrderReservationsQuery req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(req.OrderId));

        return await _db.StockReservations
            .AsNoTracking()
            .Where(r => r.OrderId == req.OrderId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new OrderReservationDto(
                r.Id,
                r.OrderId,
                r.StockItemId,
                r.Sku,
                r.Qty,
                r.CreatedAt))
            .ToListAsync(ct);
    }
}
