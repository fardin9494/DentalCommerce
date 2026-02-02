using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class RejectRefundHandler : IRequestHandler<RejectRefundCommand>
{
    private readonly ISalesDbContext _db;

    public RejectRefundHandler(ISalesDbContext db) => _db = db;

    public async Task Handle(RejectRefundCommand cmd, CancellationToken ct)
    {
        if (cmd.RefundId == Guid.Empty) throw new ArgumentException("RefundId required.", nameof(cmd.RefundId));
        if (string.IsNullOrWhiteSpace(cmd.Request.Reason)) throw new ArgumentException("Reason required.", nameof(cmd.Request.Reason));

        var refund = await _db.OrderRefunds
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == cmd.RefundId, ct);

        if (refund is null) throw new InvalidOperationException("Refund not found.");

        if (refund.Status == RefundStatus.Rejected)
        {
            await EnsureRefundTimelineAsync(refund, "RefundRejected", cmd.Request.Reason, ct);
            return;
        }

        if (refund.Status != RefundStatus.Requested && refund.Status != RefundStatus.Approved)
            throw new InvalidOperationException("Only requested/approved refunds can be rejected.");

        var reason = cmd.Request.Reason.Trim();
        var ts = DateTime.UtcNow;

        var affected = await _db.OrderRefunds
            .Where(r => r.Id == refund.Id && (r.Status == RefundStatus.Requested || r.Status == RefundStatus.Approved))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, RefundStatus.Rejected)
                .SetProperty(r => r.RejectionReason, reason)
                .SetProperty(r => r.RejectedAtUtc, ts)
                .SetProperty(r => r.UpdatedAt, ts),
                ct);

        if (affected == 0)
        {
            var latest = await _db.OrderRefunds.AsNoTracking().FirstOrDefaultAsync(r => r.Id == refund.Id, ct);
            if (latest is not null && latest.Status == RefundStatus.Rejected)
            {
                await EnsureRefundTimelineAsync(latest, "RefundRejected", reason, ct);
                return;
            }
            throw new InvalidOperationException("Refund was updated by another process.");
        }

        await EnsureRefundTimelineAsync(refund, "RefundRejected", reason, ct);
    }

    private async Task EnsureRefundTimelineAsync(OrderRefund refund, string eventType, string? message, CancellationToken ct)
    {
        var token = refund.Id.ToString();
        var exists = await _db.OrderTimeline
            .AsNoTracking()
            .AnyAsync(t =>
                t.OrderId == refund.OrderId &&
                t.EventType == eventType &&
                t.DataJson != null &&
                EF.Functions.Like(t.DataJson, $"%{token}%"),
                ct);

        if (exists) return;

        var data = JsonSerializer.Serialize(new
        {
            refundId = refund.Id,
            amount = refund.Amount,
            currency = refund.Currency,
            reason = message
        });

        _db.OrderTimeline.Add(OrderTimelineEntry.CreateExternal(refund.OrderId, eventType, null, null, message, data, DateTime.UtcNow));
        await _db.SaveChangesAsync(ct);
    }
}
