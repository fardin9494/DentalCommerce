using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.Features.Orders.Models;

namespace Sales.Application.Features.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly ISalesDbContext _db;

    public GetOrderByIdHandler(ISalesDbContext db) => _db = db;

    public async Task<OrderDto?> Handle(GetOrderByIdQuery req, CancellationToken ct)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.OrderId, ct);

        if (order is null) return null;

        return new OrderDto
        {
            Id = order.Id,
            SiteId = order.SiteId,
            UserId = order.UserId,
            OrderNumber = order.OrderNumber,
            PricingQuoteId = order.PricingQuoteId,
            Currency = order.Currency,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            PlacedAtUtc = order.PlacedAtUtc,
            CancelledAtUtc = order.CancelledAtUtc,
            PaymentFailedAtUtc = order.PaymentFailedAtUtc,
            PaymentFailureReason = order.PaymentFailureReason,
            PaymentFailureDetails = order.PaymentFailureDetails,
            Subtotal = order.Subtotal,
            DiscountTotal = order.DiscountTotal,
            FinalTotal = order.FinalTotal,
            CashbackTotal = order.CashbackTotal,
            Lines = order.Lines.Select(l => new OrderLineDto
            {
                SkuId = l.SkuId,
                BatchId = l.BatchId,
                Quantity = l.Quantity,
                CancelledQty = l.CancelledQty,
                ReturnedQty = l.ReturnedQty,
                RefundedQty = l.RefundedQty,
                BaseUnitPrice = l.BaseUnitPrice,
                FinalUnitPrice = l.FinalUnitPrice,
                IsGift = l.IsGift
            }).ToList()
        };
    }
}
