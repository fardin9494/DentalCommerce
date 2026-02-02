using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed class CompleteRefundHandler : IRequestHandler<CompleteRefundCommand>
{
    private readonly ISalesDbContext _db;

    public CompleteRefundHandler(ISalesDbContext db) => _db = db;

    public async Task Handle(CompleteRefundCommand cmd, CancellationToken ct)
    {
        if (cmd.RefundId == Guid.Empty) throw new ArgumentException("RefundId required.", nameof(cmd.RefundId));

        var refund = await _db.OrderRefunds
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == cmd.RefundId, ct);

        if (refund is null) throw new InvalidOperationException("Refund not found.");

        if (refund.Status == RefundStatus.Completed)
        {
            await EnsureRefundTimelineAsync(refund, "RefundCompleted", refund.WalletReference, ct);
            return;
        }

        if (refund.Status != RefundStatus.Approved)
            throw new InvalidOperationException("Only approved refunds can be completed.");

        var refundLines = await _db.OrderRefundLines
            .AsNoTracking()
            .Where(l => l.RefundId == refund.Id)
            .ToListAsync(ct);

        if (refundLines.Count == 0)
            throw new InvalidOperationException("Refund lines not found.");

        var orderLineIds = refundLines.Select(l => l.OrderLineId).ToList();
        var orderLines = await _db.OrderLines
            .Where(l => orderLineIds.Contains(l.Id))
            .ToListAsync(ct);

        foreach (var line in refundLines)
        {
            var orderLine = orderLines.FirstOrDefault(l => l.Id == line.OrderLineId);
            if (orderLine is null)
                throw new InvalidOperationException("Order line not found for refund.");

            orderLine.MarkRefunded(line.Quantity);
        }

        var walletRef = string.IsNullOrWhiteSpace(cmd.Request.WalletReference) ? null : cmd.Request.WalletReference.Trim();
        var ts = DateTime.UtcNow;

        var affected = await _db.OrderRefunds
            .Where(r => r.Id == refund.Id && r.Status == RefundStatus.Approved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, RefundStatus.Completed)
                .SetProperty(r => r.WalletReference, r => walletRef ?? r.WalletReference)
                .SetProperty(r => r.CompletedAtUtc, ts)
                .SetProperty(r => r.UpdatedAt, ts),
                ct);

        if (affected == 0)
        {
            var latest = await _db.OrderRefunds.AsNoTracking().FirstOrDefaultAsync(r => r.Id == refund.Id, ct);
            if (latest is not null && latest.Status == RefundStatus.Completed)
            {
                await EnsureRefundTimelineAsync(latest, "RefundCompleted", walletRef, ct);
                return;
            }
            throw new InvalidOperationException("Refund was updated by another process.");
        }

        await _db.SaveChangesAsync(ct);
        await EnsureRefundTimelineAsync(refund, "RefundCompleted", walletRef, ct);
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
            walletReference = message
        });

        _db.OrderTimeline.Add(OrderTimelineEntry.CreateExternal(refund.OrderId, eventType, null, null, message, data, DateTime.UtcNow));
        await _db.SaveChangesAsync(ct);
    }
}
