using BuildingBlocks.Domain;

namespace Sales.Domain.Orders;

public enum RefundStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
    Completed = 3
}

public enum RefundDestination
{
    Wallet = 0
}

public sealed class OrderRefund : BaseEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public RefundStatus Status { get; private set; } = RefundStatus.Requested;
    public RefundDestination Destination { get; private set; } = RefundDestination.Wallet;
    public string Currency { get; private set; } = "IRR";
    public decimal Amount { get; private set; }
    public string? Reason { get; private set; }
    public string? Note { get; private set; }
    public string? RequestedBy { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? WalletReference { get; private set; }

    private readonly List<OrderRefundLine> _lines = new();
    public IReadOnlyCollection<OrderRefundLine> Lines => _lines;

    private OrderRefund() { }

    public static OrderRefund Create(
        Guid orderId,
        string currency,
        RefundDestination destination,
        IReadOnlyCollection<OrderRefundLineDraft> lines,
        string? reason,
        string? note,
        string? requestedBy,
        DateTime? whenUtc = null)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required.", nameof(currency));
        if (lines.Count == 0) throw new ArgumentException("Refund requires at least one line.", nameof(lines));

        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

        var refund = new OrderRefund
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Currency = currency.Trim().ToUpperInvariant(),
            Destination = destination,
            Status = RefundStatus.Requested,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? null : requestedBy.Trim(),
            RequestedAtUtc = ts,
            CreatedAt = ts,
            UpdatedAt = ts
        };

        foreach (var line in lines)
        {
            refund._lines.Add(OrderRefundLine.Create(refund.Id, line));
            refund.Amount += line.LineAmount;
        }

        return refund;
    }

    public void Approve(string? note = null, DateTime? whenUtc = null)
    {
        if (Status != RefundStatus.Requested)
            throw new InvalidOperationException("Only requested refunds can be approved.");

        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

        Status = RefundStatus.Approved;
        Note = string.IsNullOrWhiteSpace(note) ? Note : note.Trim();
        ApprovedAtUtc = ts;
        Touch();
    }

    public void Reject(string reason, DateTime? whenUtc = null)
    {
        if (Status != RefundStatus.Requested && Status != RefundStatus.Approved)
            throw new InvalidOperationException("Only requested/approved refunds can be rejected.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason required.", nameof(reason));

        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

        Status = RefundStatus.Rejected;
        RejectionReason = reason.Trim();
        RejectedAtUtc = ts;
        Touch();
    }

    public void Complete(string? walletReference = null, DateTime? whenUtc = null)
    {
        if (Status != RefundStatus.Approved)
            throw new InvalidOperationException("Only approved refunds can be completed.");

        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

        Status = RefundStatus.Completed;
        CompletedAtUtc = ts;
        WalletReference = string.IsNullOrWhiteSpace(walletReference) ? WalletReference : walletReference.Trim();
        Touch();
    }
}

public sealed record OrderRefundLineDraft(
    Guid OrderLineId,
    string SkuId,
    Guid? BatchId,
    int Quantity,
    decimal UnitAmount,
    decimal LineAmount);

public sealed class OrderRefundLine : BaseEntity<Guid>
{
    public Guid RefundId { get; private set; }
    public Guid OrderLineId { get; private set; }
    public string SkuId { get; private set; } = null!;
    public Guid? BatchId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitAmount { get; private set; }
    public decimal LineAmount { get; private set; }

    private OrderRefundLine() { }

    internal static OrderRefundLine Create(Guid refundId, OrderRefundLineDraft draft)
    {
        if (refundId == Guid.Empty) throw new ArgumentException("RefundId required.", nameof(refundId));
        if (draft.OrderLineId == Guid.Empty) throw new ArgumentException("OrderLineId required.", nameof(draft.OrderLineId));
        if (string.IsNullOrWhiteSpace(draft.SkuId)) throw new ArgumentException("SkuId required.", nameof(draft));
        if (draft.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(draft.Quantity));

        return new OrderRefundLine
        {
            Id = Guid.NewGuid(),
            RefundId = refundId,
            OrderLineId = draft.OrderLineId,
            SkuId = draft.SkuId.Trim(),
            BatchId = draft.BatchId,
            Quantity = draft.Quantity,
            UnitAmount = draft.UnitAmount,
            LineAmount = draft.LineAmount
        };
    }
}
