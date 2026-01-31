using MediatR;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly ISalesDbContext _db;
    private readonly ITransactionRunner _tx;
    private readonly IPricingQuoteGateway _pricing;

    public CreateOrderHandler(ISalesDbContext db, ITransactionRunner tx, IPricingQuoteGateway pricing)
    {
        _db = db;
        _tx = tx;
        _pricing = pricing;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        return await _tx.ExecuteAsync(async token =>
        {
            var quote = await _pricing.CreateQuoteAsync(
                new PricingQuoteRequest(
                    req.SiteId,
                    req.UserId,
                    req.CouponCode,
                    req.Items.Select(i => new PricingQuoteRequestItem(i.SkuId, i.Qty, i.BatchId)).ToList()),
                token);

            var lineDrafts = quote.Lines
                .Select(l => new OrderLineDraft(
                    l.SkuId,
                    l.BatchId,
                    l.Quantity,
                    l.BaseUnitPrice,
                    l.FinalUnitPrice,
                    l.IsGift,
                    l.AdjustmentsJson))
                .ToList();

            var order = Order.Place(
                quote.SiteId,
                quote.UserId,
                quote.QuoteId,
                quote.Currency,
                quote.TimestampUtc,
                lineDrafts,
                quote.Subtotal,
                quote.DiscountTotal,
                quote.FinalTotal,
                quote.CashbackTotal);

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(token);

            return new CreateOrderResult(
                order.Id,
                order.Currency,
                order.Subtotal,
                order.DiscountTotal,
                order.FinalTotal,
                order.CashbackTotal);
        }, ct);
    }
}

