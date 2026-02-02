using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.Features.Orders.Models;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Queries;

public sealed record GetOrderTimelineQuery(Guid OrderId) : IRequest<IReadOnlyList<OrderTimelineDto>>;

public sealed class GetOrderTimelineHandler : IRequestHandler<GetOrderTimelineQuery, IReadOnlyList<OrderTimelineDto>>
{
    private readonly ISalesDbContext _db;

    public GetOrderTimelineHandler(ISalesDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderTimelineDto>> Handle(GetOrderTimelineQuery req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(req.OrderId));

        var timeline = await _db.OrderTimeline
            .AsNoTracking()
            .Where(x => x.OrderId == req.OrderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new OrderTimelineDto(
                x.Id,
                x.OrderId,
                x.EventType,
                x.FromStatus.HasValue ? x.FromStatus.Value.ToString() : null,
                x.ToStatus.HasValue ? x.ToStatus.Value.ToString() : null,
                x.Message,
                x.DataJson,
                x.CreatedAt))
            .ToListAsync(ct);

        if (timeline.Count > 1)
            return timeline;

        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);

        if (order is null)
            return timeline;

        var items = new List<OrderTimelineDto>(timeline);
        var existing = new HashSet<string>(items.Select(x => x.EventType), StringComparer.OrdinalIgnoreCase);

        void AddIfMissing(string eventType, string? from, string? to, DateTime? when)
        {
            if (existing.Contains(eventType)) return;
            var ts = when ?? order.UpdatedAt;
            items.Add(new OrderTimelineDto(Guid.NewGuid(), order.Id, eventType, from, to, null, null, ts));
            existing.Add(eventType);
        }

        if (!existing.Contains("Created"))
            AddIfMissing("Created", null, OrderStatus.Draft.ToString(), order.CreatedAt);

        if (order.PlacedAtUtc.HasValue || order.Status is OrderStatus.Placed or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Returned or OrderStatus.Refunded)
            AddIfMissing("Placed", OrderStatus.Draft.ToString(), OrderStatus.Placed.ToString(), order.PlacedAtUtc);

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Returned or OrderStatus.Refunded)
            AddIfMissing("Shipped", OrderStatus.Placed.ToString(), OrderStatus.Shipped.ToString(), order.ShippedAtUtc);

        if (order.Status is OrderStatus.Delivered or OrderStatus.Returned or OrderStatus.Refunded)
            AddIfMissing("Delivered", OrderStatus.Shipped.ToString(), OrderStatus.Delivered.ToString(), order.DeliveredAtUtc);

        if (order.Status is OrderStatus.Returned or OrderStatus.Refunded)
            AddIfMissing("Returned", OrderStatus.Delivered.ToString(), OrderStatus.Returned.ToString(), order.ReturnedAtUtc);

        if (order.Status is OrderStatus.Refunded)
            AddIfMissing("Refunded", null, OrderStatus.Refunded.ToString(), order.RefundedAtUtc);

        if (order.Status is OrderStatus.Cancelled)
            AddIfMissing("Cancelled", null, OrderStatus.Cancelled.ToString(), order.CancelledAtUtc);

        if (order.Status is OrderStatus.PaymentFailed)
            AddIfMissing("PaymentFailed", OrderStatus.Draft.ToString(), OrderStatus.PaymentFailed.ToString(), order.PaymentFailedAtUtc);

        return items
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }
}
