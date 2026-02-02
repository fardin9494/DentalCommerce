using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.Features.Orders.Models;

namespace Sales.Application.Features.Orders.Queries;

public sealed record GetOrderRefundsQuery(Guid OrderId) : IRequest<IReadOnlyList<OrderRefundDto>>;

public sealed class GetOrderRefundsHandler : IRequestHandler<GetOrderRefundsQuery, IReadOnlyList<OrderRefundDto>>
{
    private readonly ISalesDbContext _db;

    public GetOrderRefundsHandler(ISalesDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderRefundDto>> Handle(GetOrderRefundsQuery req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(req.OrderId));

        var refunds = await _db.OrderRefunds
            .AsNoTracking()
            .Where(r => r.OrderId == req.OrderId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new OrderRefundDto(
                r.Id,
                r.OrderId,
                r.Status.ToString(),
                r.Destination.ToString(),
                r.Currency,
                r.Amount,
                r.Reason,
                r.Note,
                r.RequestedBy,
                r.RequestedAtUtc,
                r.ApprovedAtUtc,
                r.RejectedAtUtc,
                r.CompletedAtUtc,
                r.RejectionReason,
                r.WalletReference,
                new List<OrderRefundLineDto>()))
            .ToListAsync(ct);

        if (refunds.Count == 0)
            return refunds;

        var refundIds = refunds.Select(r => r.Id).ToList();
        var lines = await _db.OrderRefundLines
            .AsNoTracking()
            .Where(l => refundIds.Contains(l.RefundId))
            .Select(l => new
            {
                l.RefundId,
                Dto = new OrderRefundLineDto(
                    l.Id,
                    l.OrderLineId,
                    l.SkuId,
                    l.BatchId,
                    l.Quantity,
                    l.UnitAmount,
                    l.LineAmount)
            })
            .ToListAsync(ct);

        var lineMap = lines
            .GroupBy(x => x.RefundId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OrderRefundLineDto>)g.Select(x => x.Dto).ToList());

        return refunds
            .Select(r => r with { Lines = lineMap.TryGetValue(r.Id, out var l) ? l : Array.Empty<OrderRefundLineDto>() })
            .ToList();
    }
}
