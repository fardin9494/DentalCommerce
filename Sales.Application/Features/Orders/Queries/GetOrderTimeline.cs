using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.Features.Orders.Models;

namespace Sales.Application.Features.Orders.Queries;

public sealed record GetOrderTimelineQuery(Guid OrderId) : IRequest<IReadOnlyList<OrderTimelineDto>>;

public sealed class GetOrderTimelineHandler : IRequestHandler<GetOrderTimelineQuery, IReadOnlyList<OrderTimelineDto>>
{
    private readonly ISalesDbContext _db;

    public GetOrderTimelineHandler(ISalesDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderTimelineDto>> Handle(GetOrderTimelineQuery req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(req.OrderId));

        return await _db.OrderTimeline
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
    }
}
