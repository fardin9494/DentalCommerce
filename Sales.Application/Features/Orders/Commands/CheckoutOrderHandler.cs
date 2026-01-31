using MediatR;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, CheckoutOrderResult>
{
    private readonly ISalesDbContext _db;
    private readonly IPricingQuoteGateway _pricing;
    private readonly IPaymentGateway _payment;
    private readonly IInventoryReservationGateway _inventory;

    public CheckoutOrderHandler(
        ISalesDbContext db,
        IPricingQuoteGateway pricing,
        IPaymentGateway payment,
        IInventoryReservationGateway inventory)
    {
        _db = db;
        _pricing = pricing;
        _payment = payment;
        _inventory = inventory;
    }

    public async Task<CheckoutOrderResult> Handle(CheckoutOrderCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        var quote = await _pricing.CreateQuoteAsync(
            new PricingQuoteRequest(
                req.SiteId,
                req.UserId,
                req.CouponCode,
                req.Items.Select(i => new PricingQuoteRequestItem(i.SkuId, i.Qty, i.BatchId)).ToList()),
            ct);

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

        var order = Order.CreateDraft(
            quote.SiteId,
            quote.UserId,
            quote.QuoteId,
            quote.Currency,
            lineDrafts,
            quote.Subtotal,
            quote.DiscountTotal,
            quote.FinalTotal,
            quote.CashbackTotal);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        var paymentResult = await _payment.ChargeAsync(
            new PaymentRequest(order.Id, order.FinalTotal, order.Currency, req.PaymentScenario),
            ct);

        if (!paymentResult.Success)
        {
            order.MarkPaymentFailed(paymentResult.FailureReason, paymentResult.FailureReason);
            await _db.SaveChangesAsync(ct);
            try
            {
                await _inventory.ReleaseAsync(order.Id, ct);
            }
            catch
            {
                // ignore release errors in test flow
            }

            return new CheckoutOrderResult(
                order.Id,
                order.Status.ToString(),
                order.Currency,
                order.Subtotal,
                order.DiscountTotal,
                order.FinalTotal,
                order.CashbackTotal,
                paymentResult.FailureReason);
        }

        try
        {
            var reservationLines = order.Lines
                .GroupBy(l => l.SkuId, StringComparer.OrdinalIgnoreCase)
                .Select(g => new InventoryReservationRequestLine(
                    g.Key,
                    g.Sum(x => (decimal)x.Quantity)))
                .ToList();

            await _inventory.ReserveAsync(order.Id, reservationLines, ct);
        }
        catch (Exception ex)
        {
            order.MarkPaymentFailed(ex.Message, ex.ToString());
            await _db.SaveChangesAsync(ct);
            try
            {
                await _inventory.ReleaseAsync(order.Id, ct);
            }
            catch
            {
                // ignore release errors in test flow
            }

            return new CheckoutOrderResult(
                order.Id,
                order.Status.ToString(),
                order.Currency,
                order.Subtotal,
                order.DiscountTotal,
                order.FinalTotal,
                order.CashbackTotal,
                ex.Message);
        }

        order.MarkPlaced();
        await _db.SaveChangesAsync(ct);

        return new CheckoutOrderResult(
            order.Id,
            order.Status.ToString(),
            order.Currency,
            order.Subtotal,
            order.DiscountTotal,
            order.FinalTotal,
            order.CashbackTotal);
    }
}
