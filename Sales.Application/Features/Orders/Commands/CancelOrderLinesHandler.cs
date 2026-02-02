using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class CancelOrderLinesHandler : IRequestHandler<CancelOrderLinesCommand, CancelOrderLinesResult>
{
    private readonly ISalesDbContext _db;
    private readonly IInventoryReservationGateway _inventory;

    public CancelOrderLinesHandler(ISalesDbContext db, IInventoryReservationGateway inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    public async Task<CancelOrderLinesResult> Handle(CancelOrderLinesCommand cmd, CancellationToken ct)
    {
        if (cmd.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(cmd.OrderId));
        if (cmd.Request.Lines.Count == 0) throw new ArgumentException("At least one line is required.", nameof(cmd.Request.Lines));

        static (OrderRefund refund, List<object> detailLines) ApplyChanges(
            Order order,
            CancelOrderLinesRequest request,
            ISalesDbContext db)
        {
            var refundLines = new List<OrderRefundLineDraft>();
            var detailLines = new List<object>();

            foreach (var reqLine in request.Lines)
            {
                if (string.IsNullOrWhiteSpace(reqLine.SkuId))
                    throw new ArgumentException("SkuId required.", nameof(request.Lines));
                if (reqLine.Qty <= 0)
                    throw new ArgumentOutOfRangeException(nameof(reqLine.Qty));

                var line = order.Lines.FirstOrDefault(l =>
                    string.Equals(l.SkuId, reqLine.SkuId, StringComparison.OrdinalIgnoreCase) &&
                    l.BatchId == reqLine.BatchId);

                if (line is null)
                    throw new InvalidOperationException($"Order line not found for SKU '{reqLine.SkuId}'.");

                if (reqLine.Qty > line.ActiveQty)
                    throw new InvalidOperationException("Cancel quantity exceeds available items.");

                line.Cancel(reqLine.Qty);

                var unitAmount = line.FinalUnitPrice;
                var lineAmount = unitAmount * reqLine.Qty;
                refundLines.Add(new OrderRefundLineDraft(
                    line.Id,
                    line.SkuId,
                    line.BatchId,
                    reqLine.Qty,
                    unitAmount,
                    lineAmount));

                detailLines.Add(new
                {
                    line.SkuId,
                    line.BatchId,
                    qty = reqLine.Qty,
                    unitAmount,
                    lineAmount
                });
            }

            order.RecalculateTotalsFromLines();

            if (order.Lines.All(l => l.ActiveQty <= 0))
            {
                order.Cancel(request.Reason, request.Note);
            }

            var refund = OrderRefund.Create(
                order.Id,
                order.Currency,
                RefundDestination.Wallet,
                refundLines,
                request.Reason,
                request.Note,
                request.RequestedBy);

            db.OrderRefunds.Add(refund);

            var cancelData = JsonSerializer.Serialize(new
            {
                lines = detailLines,
                reason = request.Reason,
                note = request.Note,
                refundId = refund.Id,
                amount = refund.Amount,
                destination = refund.Destination.ToString()
            });

            var refundData = JsonSerializer.Serialize(new
            {
                refundId = refund.Id,
                amount = refund.Amount,
                currency = refund.Currency,
                destination = refund.Destination.ToString()
            });

            var ts = DateTime.UtcNow;
            db.OrderTimeline.Add(OrderTimelineEntry.CreateExternal(order.Id, "LineCancelled", order.Status, order.Status, request.Reason, cancelData, ts));
            db.OrderTimeline.Add(OrderTimelineEntry.CreateExternal(order.Id, "RefundRequested", order.Status, order.Status, request.Reason, refundData, ts));
            OrderCommandHelpers.EnsureLatestTimelineTracked(db, order);

            return (refund, detailLines);
        }

        const int maxAttempts = 2;
        DbUpdateConcurrencyException? lastEx = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var order = await _db.Orders
                .Include(o => o.Lines)
                .Include(o => o.Timeline)
                .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

            if (order is null) throw new InvalidOperationException("Order not found.");
            if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.Placed)
                throw new InvalidOperationException("Only draft or placed orders can be edited.");

            var (refund, _) = ApplyChanges(order, cmd.Request, _db);

            try
            {
                await _db.SaveChangesAsync(ct);
                await RefreshReservationsAsync(order, ct);
                return new CancelOrderLinesResult(order.Id, refund.Id, refund.Amount);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                lastEx = ex;
                _db.ChangeTracker.Clear();
                if (attempt < maxAttempts) continue;
                break;
            }
        }

        // Fallback: force update order fields to bypass concurrency, then persist lines/refund/timeline.
        var freshOrder = await _db.Orders
            .Include(o => o.Lines)
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (freshOrder is null) throw new InvalidOperationException("Order not found.");
        if (freshOrder.Status != OrderStatus.Draft && freshOrder.Status != OrderStatus.Placed)
            throw new InvalidOperationException("Only draft or placed orders can be edited.");

        var (fallbackRefund, _) = ApplyChanges(freshOrder, cmd.Request, _db);

        var status = freshOrder.Status;
        var subtotal = freshOrder.Subtotal;
        var finalTotal = freshOrder.FinalTotal;
        var discountTotal = freshOrder.DiscountTotal;
        var updatedAt = freshOrder.UpdatedAt;
        var cancelledAtUtc = freshOrder.CancelledAtUtc;

        var affected = await _db.Orders
            .Where(o => o.Id == freshOrder.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, status)
                .SetProperty(o => o.Subtotal, subtotal)
                .SetProperty(o => o.FinalTotal, finalTotal)
                .SetProperty(o => o.DiscountTotal, discountTotal)
                .SetProperty(o => o.UpdatedAt, updatedAt)
                .SetProperty(o => o.CancelledAtUtc, cancelledAtUtc),
                ct);

        if (affected == 0)
            throw new InvalidOperationException("Order not found.", lastEx);

        _db.Entry(freshOrder).State = EntityState.Detached;
        await _db.SaveChangesAsync(ct);

        await RefreshReservationsAsync(freshOrder, ct);
        return new CancelOrderLinesResult(freshOrder.Id, fallbackRefund.Id, fallbackRefund.Amount);
    }

    private async Task RefreshReservationsAsync(Order order, CancellationToken ct)
    {
        if (order.Status != OrderStatus.Placed)
            return;

        var remaining = order.Lines
            .Where(l => l.ActiveQty > 0)
            .GroupBy(l => l.SkuId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new InventoryReservationRequestLine(
                g.Key,
                g.Sum(x => (decimal)x.ActiveQty)))
            .ToList();

        try
        {
            await _inventory.ReleaseAsync(order.Id, ct);
            if (remaining.Count > 0)
                await _inventory.ReserveAsync(order.Id, remaining, ct);
        }
        catch (Exception ex)
        {
            order.LogEvent("ReservationUpdateFailed", ex.Message, ex.ToString());
            OrderCommandHelpers.EnsureLatestTimelineTracked(_db, order);
            await _db.SaveChangesAsync(ct);
            throw;
        }
    }
}
