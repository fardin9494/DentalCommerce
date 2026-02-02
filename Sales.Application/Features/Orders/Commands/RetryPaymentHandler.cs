using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class RetryPaymentHandler : IRequestHandler<RetryPaymentCommand, RetryPaymentResult>
{
    private readonly ISalesDbContext _db;
    private readonly IPaymentGateway _payment;
    private readonly IInventoryReservationGateway _inventory;

    public RetryPaymentHandler(
        ISalesDbContext db,
        IPaymentGateway payment,
        IInventoryReservationGateway inventory)
    {
        _db = db;
        _payment = payment;
        _inventory = inventory;
    }

    public async Task<RetryPaymentResult> Handle(RetryPaymentCommand cmd, CancellationToken ct)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.Lines)
                    .Include(o => o.Timeline)
                    .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

                if (order is null)
                    throw new InvalidOperationException($"Order {cmd.OrderId} not found.");

                // Only allow retry for PaymentFailed or Draft orders
                if (order.Status != OrderStatus.PaymentFailed && order.Status != OrderStatus.Draft)
                {
                    return new RetryPaymentResult(
                        false,
                        $"Cannot retry payment for order in status: {order.Status}",
                        order.Status.ToString());
                }

                // Retry payment with Success scenario (for fake gateway, this will always succeed)
                var paymentResult = await _payment.ChargeAsync(
                    new PaymentRequest(order.Id, order.FinalTotal, order.Currency, PaymentScenario.Success),
                    ct);

                if (!paymentResult.Success)
                {
                    order.MarkPaymentFailed(paymentResult.FailureReason, paymentResult.FailureReason);
                    OrderCommandHelpers.EnsureLatestTimelineTracked(_db, order);
                    await _db.SaveChangesAsync(ct);
                    
                    try
                    {
                        await _inventory.ReleaseAsync(order.Id, ct);
                    }
                    catch
                    {
                        // ignore release errors
                    }

                    return new RetryPaymentResult(
                        false,
                        paymentResult.FailureReason,
                        order.Status.ToString());
                }

                // Payment succeeded - reserve inventory
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
                    // If inventory reservation fails, mark payment as failed
                    order.MarkPaymentFailed(ex.Message, ex.ToString());
                    OrderCommandHelpers.EnsureLatestTimelineTracked(_db, order);
                    await _db.SaveChangesAsync(ct);
                    
                    try
                    {
                        await _inventory.ReleaseAsync(order.Id, ct);
                    }
                    catch
                    {
                        // ignore release errors
                    }

                    return new RetryPaymentResult(
                        false,
                        ex.Message,
                        order.Status.ToString());
                }

                // Mark order as placed
                order.MarkPlaced();
                OrderCommandHelpers.EnsureLatestTimelineTracked(_db, order);
                await _db.SaveChangesAsync(ct);

                return new RetryPaymentResult(
                    true,
                    null,
                    order.Status.ToString());
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
        }

        // If all attempts failed, throw the last exception
        throw new InvalidOperationException($"Failed to retry payment after {maxAttempts} attempts.");
    }
}
