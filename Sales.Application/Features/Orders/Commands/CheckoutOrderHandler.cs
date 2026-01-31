using MediatR;
using Microsoft.EntityFrameworkCore;
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

        PricingQuoteSnapshot quote;
        try
        {
            quote = await _pricing.CreateQuoteAsync(
                new PricingQuoteRequest(
                    req.SiteId,
                    req.UserId,
                    req.CouponCode,
                    req.Items.Select(i => new PricingQuoteRequestItem(i.SkuId, i.Qty, i.BatchId)).ToList()),
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Price list missing SKU", StringComparison.OrdinalIgnoreCase))
        {
            var sku = ExtractSku(ex.Message);
            var msg = string.IsNullOrWhiteSpace(sku)
                ? "کالای انتخابی در لیست قیمت فعال وجود ندارد."
                : $"SKU '{sku}' در لیست قیمت فعال وجود ندارد.";
            throw new InvalidOperationException(msg);
        }

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
            order = await ApplyOrderUpdateAsync(
                order.Id,
                o => o.MarkPaymentFailed(paymentResult.FailureReason, paymentResult.FailureReason),
                o => o.Status == OrderStatus.PaymentFailed,
                ct);
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
            order = await ApplyOrderUpdateAsync(
                order.Id,
                o => o.MarkPaymentFailed(ex.Message, ex.ToString()),
                o => o.Status == OrderStatus.PaymentFailed,
                ct);
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

        order = await ApplyOrderUpdateAsync(
            order.Id,
            o => o.MarkPlaced(),
            o => o.Status == OrderStatus.Placed,
            ct);

        return new CheckoutOrderResult(
            order.Id,
            order.Status.ToString(),
            order.Currency,
            order.Subtotal,
            order.DiscountTotal,
            order.FinalTotal,
            order.CashbackTotal);
    }

    private async Task<Order> ApplyOrderUpdateAsync(
        Guid orderId,
        Action<Order> apply,
        Func<Order, bool> isSatisfied,
        CancellationToken ct)
    {
        const int maxAttempts = 3;
        DbUpdateConcurrencyException? lastEx = null;
        Order? lastApplied = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var current = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (current is null) throw new InvalidOperationException("Order not found.");

            if (isSatisfied(current))
                return current;

            apply(current);
            lastApplied = current;

            try
            {
                await _db.SaveChangesAsync(ct);
                return current;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                lastEx = ex;
                _db.ChangeTracker.Clear();
                if (attempt == maxAttempts) break;
            }
        }

        var latest = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (latest is not null && isSatisfied(latest))
            return latest;

        if (lastApplied is null)
            throw new InvalidOperationException("رکورد توسط کاربر دیگری تغییر یافته است. لطفا دوباره تلاش کنید.", lastEx);

        var pendingTimeline = _db.ChangeTracker.Entries<OrderTimelineEntry>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        var affected = await _db.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, lastApplied.Status)
                .SetProperty(o => o.UpdatedAt, lastApplied.UpdatedAt)
                .SetProperty(o => o.PlacedAtUtc, lastApplied.PlacedAtUtc)
                .SetProperty(o => o.PaymentFailedAtUtc, lastApplied.PaymentFailedAtUtc)
                .SetProperty(o => o.PaymentFailureReason, lastApplied.PaymentFailureReason)
                .SetProperty(o => o.PaymentFailureDetails, lastApplied.PaymentFailureDetails),
                ct);

        if (affected == 0)
            throw new InvalidOperationException("رکورد توسط کاربر دیگری تغییر یافته است. لطفا دوباره تلاش کنید.", lastEx);

        _db.ChangeTracker.Clear();
        if (pendingTimeline.Count > 0)
        {
            _db.OrderTimeline.AddRange(pendingTimeline);
            await _db.SaveChangesAsync(ct);
        }

        var refreshed = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
        return refreshed ?? lastApplied;
    }

    private static string? ExtractSku(string message)
    {
        var marker = "Price list missing SKU";
        var idx = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var sku = message[(idx + marker.Length)..].Trim();
        if (sku.StartsWith(':')) sku = sku[1..].Trim();
        if (sku.EndsWith('.')) sku = sku[..^1].Trim();
        return string.IsNullOrWhiteSpace(sku) ? null : sku;
    }
}
