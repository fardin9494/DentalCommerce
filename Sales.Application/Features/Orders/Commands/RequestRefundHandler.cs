using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class RequestRefundHandler : IRequestHandler<RequestRefundCommand, RequestRefundResult>
{
    private readonly ISalesDbContext _db;

    public RequestRefundHandler(ISalesDbContext db) => _db = db;

    public async Task<RequestRefundResult> Handle(RequestRefundCommand cmd, CancellationToken ct)
    {
        if (cmd.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(cmd.OrderId));
        if (cmd.Request.Lines.Count == 0) throw new ArgumentException("At least one line is required.", nameof(cmd.Request.Lines));

        var order = await _db.Orders
            .Include(o => o.Lines)
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (order is null) throw new InvalidOperationException("Order not found.");

        if (order.Status == OrderStatus.Draft)
            throw new InvalidOperationException("Refund is not allowed for draft orders.");

        var orderLineIds = order.Lines.Select(l => l.Id).ToList();
        var pendingByLine = await _db.OrderRefundLines
            .Where(l => orderLineIds.Contains(l.OrderLineId))
            .Join(_db.OrderRefunds,
                l => l.RefundId,
                r => r.Id,
                (l, r) => new { l, r })
            .Where(x => x.r.Status == RefundStatus.Requested || x.r.Status == RefundStatus.Approved)
            .GroupBy(x => x.l.OrderLineId)
            .Select(g => new { LineId = g.Key, Qty = g.Sum(x => x.l.Quantity) })
            .ToDictionaryAsync(x => x.LineId, x => x.Qty, ct);

        var refundLines = new List<OrderRefundLineDraft>();
        var detailLines = new List<object>();

        foreach (var reqLine in cmd.Request.Lines)
        {
            if (string.IsNullOrWhiteSpace(reqLine.SkuId))
                throw new ArgumentException("SkuId required.", nameof(cmd.Request.Lines));
            if (reqLine.Qty <= 0)
                throw new ArgumentOutOfRangeException(nameof(reqLine.Qty));

            var line = order.Lines.FirstOrDefault(l =>
                string.Equals(l.SkuId, reqLine.SkuId, StringComparison.OrdinalIgnoreCase) &&
                l.BatchId == reqLine.BatchId);

            if (line is null)
                throw new InvalidOperationException($"Order line not found for SKU '{reqLine.SkuId}'.");

            var pendingQty = pendingByLine.TryGetValue(line.Id, out var q) ? q : 0;
            var availableQty = line.Quantity - line.RefundedQty - pendingQty;

            if (reqLine.Qty > availableQty)
                throw new InvalidOperationException("Refund quantity exceeds available items.");

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

        var refund = OrderRefund.Create(
            order.Id,
            order.Currency,
            RefundDestination.Wallet,
            refundLines,
            cmd.Request.Reason,
            cmd.Request.Note,
            cmd.Request.RequestedBy);

        _db.OrderRefunds.Add(refund);

        var refundData = JsonSerializer.Serialize(new
        {
            refundId = refund.Id,
            amount = refund.Amount,
            currency = refund.Currency,
            destination = refund.Destination.ToString(),
            lines = detailLines,
            reason = cmd.Request.Reason
        });

        order.LogEvent("RefundRequested", cmd.Request.Reason, refundData);
        OrderCommandHelpers.EnsureLatestTimelineTracked(_db, order);
        await _db.SaveChangesAsync(ct);

        return new RequestRefundResult(refund.Id, refund.Amount, refund.Currency);
    }
}
