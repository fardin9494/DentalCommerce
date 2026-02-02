using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class ApproveRefundHandler : IRequestHandler<ApproveRefundCommand>
{
    private readonly ISalesDbContext _db;

    public ApproveRefundHandler(ISalesDbContext db) => _db = db;

    public async Task Handle(ApproveRefundCommand cmd, CancellationToken ct)
    {
        if (cmd.RefundId == Guid.Empty) throw new ArgumentException("RefundId required.", nameof(cmd.RefundId));

        var refund = await _db.OrderRefunds
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == cmd.RefundId, ct);

        if (refund is null) throw new InvalidOperationException("Refund not found.");

        if (refund.Status == RefundStatus.Approved)
        {
            await EnsureRefundTimelineAsync(refund, "RefundApproved", cmd.Request.Note, ct);
            return;
        }

        if (refund.Status != RefundStatus.Requested)
            throw new InvalidOperationException("Only requested refunds can be approved.");

        var note = string.IsNullOrWhiteSpace(cmd.Request.Note) ? null : cmd.Request.Note.Trim();
        var ts = DateTime.UtcNow;

        var affected = await _db.OrderRefunds
            .Where(r => r.Id == refund.Id && r.Status == RefundStatus.Requested)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, RefundStatus.Approved)
                .SetProperty(r => r.Note, r => note ?? r.Note)
                .SetProperty(r => r.ApprovedAtUtc, ts)
                .SetProperty(r => r.UpdatedAt, ts),
                ct);

        if (affected == 0)
        {
            var latest = await _db.OrderRefunds.AsNoTracking().FirstOrDefaultAsync(r => r.Id == refund.Id, ct);
            if (latest is not null && latest.Status == RefundStatus.Approved)
            {
                await EnsureRefundTimelineAsync(latest, "RefundApproved", note, ct);
                return;
            }
            throw new InvalidOperationException("Refund was updated by another process.");
        }

        await EnsureRefundTimelineAsync(refund, "RefundApproved", note, ct);
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
            currency = refund.Currency
        });

        _db.OrderTimeline.Add(OrderTimelineEntry.CreateExternal(refund.OrderId, eventType, null, null, message, data, DateTime.UtcNow));
        await _db.SaveChangesAsync(ct);
    }
}
