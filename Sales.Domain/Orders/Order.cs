using BuildingBlocks.Domain;

namespace Sales.Domain.Orders;

public sealed class Order : AggregateRoot<Guid>
{
    public Guid SiteId { get; private set; }
    public Guid? UserId { get; private set; }
    public string OrderNumber { get; private set; } = null!;

    public Guid PricingQuoteId { get; private set; }
    public string Currency { get; private set; } = "IRR";

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public DateTime? PlacedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime? PaymentFailedAtUtc { get; private set; }
    public DateTime? ShippedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? ReturnedAtUtc { get; private set; }
    public DateTime? RefundedAtUtc { get; private set; }
    public string? PaymentFailureReason { get; private set; }
    public string? PaymentFailureDetails { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal FinalTotal { get; private set; }
    public decimal CashbackTotal { get; private set; }

    private readonly List<OrderLine> _lines = new();
    public IReadOnlyCollection<OrderLine> Lines => _lines;

    private readonly List<OrderTimelineEntry> _timeline = new();
    public IReadOnlyCollection<OrderTimelineEntry> Timeline => _timeline;

    private Order() { }

    public static Order CreateDraft(
        Guid siteId,
        Guid? userId,
        Guid pricingQuoteId,
        string currency,
        IReadOnlyCollection<OrderLineDraft> lines,
        decimal subtotal,
        decimal discountTotal,
        decimal finalTotal,
        decimal cashbackTotal)
    {
        if (siteId == Guid.Empty) throw new ArgumentException("SiteId required.", nameof(siteId));
        if (pricingQuoteId == Guid.Empty) throw new ArgumentException("PricingQuoteId required.", nameof(pricingQuoteId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required.", nameof(currency));
        if (lines.Count == 0) throw new ArgumentException("Order requires at least one line.", nameof(lines));

        var order = new Order
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            PricingQuoteId = pricingQuoteId,
            Currency = currency.Trim().ToUpperInvariant(),
            Status = OrderStatus.Draft,
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            FinalTotal = finalTotal,
            CashbackTotal = cashbackTotal
        };

        foreach (var l in lines)
            order._lines.Add(OrderLine.Create(order.Id, l));

        order.AddTimeline("Created", null, OrderStatus.Draft, null, null);
        return order;
    }

    public static Order Place(
        Guid siteId,
        Guid? userId,
        Guid pricingQuoteId,
        string currency,
        DateTime placedAtUtc,
        IReadOnlyCollection<OrderLineDraft> lines,
        decimal subtotal,
        decimal discountTotal,
        decimal finalTotal,
        decimal cashbackTotal)
    {
        var order = CreateDraft(
            siteId,
            userId,
            pricingQuoteId,
            currency,
            lines,
            subtotal,
            discountTotal,
            finalTotal,
            cashbackTotal);
        order.MarkPlaced(placedAtUtc);
        return order;
    }

    public void MarkPlaced(DateTime? whenUtc = null)
    {
        if (Status != OrderStatus.Draft && Status != OrderStatus.PaymentFailed)
            throw new InvalidOperationException("Order is not in a placeable state.");

        var from = Status;
        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
        Status = OrderStatus.Placed;
        PlacedAtUtc = ts;
        PaymentFailedAtUtc = null;
        PaymentFailureReason = null;
        PaymentFailureDetails = null;
        Touch();
        AddTimeline("Placed", from, Status, null, null, ts);
    }

    public void MarkPaymentFailed(string? reason = null, string? details = null, DateTime? whenUtc = null)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Only draft orders can fail payment.");

        var from = Status;
        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
        Status = OrderStatus.PaymentFailed;
        PaymentFailedAtUtc = ts;
        PaymentFailureReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        PaymentFailureDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        Touch();
        AddTimeline("PaymentFailed", from, Status, PaymentFailureReason, PaymentFailureDetails, ts);
    }

    public void Cancel(string? reason = null, string? details = null, DateTime? whenUtc = null)
    {
        if (Status != OrderStatus.Placed && Status != OrderStatus.Draft)
            throw new InvalidOperationException("Only draft or placed orders can be cancelled.");

        var from = Status;
        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
        Status = OrderStatus.Cancelled;
        CancelledAtUtc = ts;
        Touch();
        AddTimeline("Cancelled", from, Status, reason, details, ts);
    }

    public void MarkShipped(string? message = null, string? dataJson = null, DateTime? whenUtc = null)
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException("Only placed orders can be shipped.");

        var from = Status;
        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
        Status = OrderStatus.Shipped;
        ShippedAtUtc = ts;
        Touch();
        AddTimeline("Shipped", from, Status, message, dataJson, ts);
    }

    public void MarkDelivered(string? message = null, string? dataJson = null, DateTime? whenUtc = null)
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be delivered.");

        var from = Status;
        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
        Status = OrderStatus.Delivered;
        DeliveredAtUtc = ts;
        Touch();
        AddTimeline("Delivered", from, Status, message, dataJson, ts);
    }

    public void MarkReturned(string? message = null, string? dataJson = null, DateTime? whenUtc = null)
    {
        if (Status != OrderStatus.Delivered)
            throw new InvalidOperationException("Only delivered orders can be returned.");

        var from = Status;
        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
        Status = OrderStatus.Returned;
        ReturnedAtUtc = ts;
        Touch();
        AddTimeline("Returned", from, Status, message, dataJson, ts);
    }

    public void MarkRefunded(string? message = null, string? dataJson = null, DateTime? whenUtc = null)
    {
        if (Status != OrderStatus.Returned && Status != OrderStatus.Delivered && Status != OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is not in a refundable state.");

        var from = Status;
        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
        Status = OrderStatus.Refunded;
        RefundedAtUtc = ts;
        Touch();
        AddTimeline("Refunded", from, Status, message, dataJson, ts);
    }

    public void RecalculateTotalsFromLines()
    {
        decimal subtotal = 0;
        decimal finalTotal = 0;

        foreach (var line in _lines)
        {
            if (line.ActiveQty <= 0) continue;
            subtotal += line.BaseUnitPrice * line.ActiveQty;
            finalTotal += line.FinalUnitPrice * line.ActiveQty;
        }

        Subtotal = subtotal;
        FinalTotal = finalTotal;
        DiscountTotal = Math.Max(0, Subtotal - FinalTotal);
        Touch();
    }

    public void EnsureTimelineEvent(string eventType, string? message = null, string? dataJson = null, DateTime? whenUtc = null)
    {
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType required.", nameof(eventType));
        if (_timeline.Any(t => string.Equals(t.EventType, eventType, StringComparison.OrdinalIgnoreCase)))
            return;

        AddTimeline(eventType, Status, Status, message, dataJson, whenUtc);
    }

    public void LogEvent(string eventType, string? message = null, string? dataJson = null, DateTime? whenUtc = null)
    {
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType required.", nameof(eventType));
        AddTimeline(eventType, Status, Status, message, dataJson, whenUtc);
    }

    public void LogNoteAdded(Guid noteId, string? createdBy, bool isInternal, DateTime? whenUtc = null)
    {
        if (noteId == Guid.Empty) throw new ArgumentException("NoteId required.", nameof(noteId));

        var who = string.IsNullOrWhiteSpace(createdBy) ? null : createdBy.Trim();
        var label = isInternal ? "Internal note added" : "Note added";
        var message = who is null ? label : $"{label} by {who}";
        var data = $"{{\"noteId\":\"{noteId}\",\"isInternal\":{isInternal.ToString().ToLowerInvariant()}}}";

        AddTimeline("NoteAdded", Status, Status, message, data, whenUtc);
    }

    private void AddTimeline(
        string eventType,
        OrderStatus? fromStatus,
        OrderStatus? toStatus,
        string? message,
        string? dataJson,
        DateTime? whenUtc = null)
    {
        _timeline.Add(OrderTimelineEntry.Create(
            Id,
            eventType,
            fromStatus,
            toStatus,
            message,
            dataJson,
            whenUtc));
    }

    private static string GenerateOrderNumber()
    {
        var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var tail = Guid.NewGuid().ToString("N")[^4..].ToUpperInvariant();
        return $"SO-{ts}-{tail}";
    }
}

public sealed record OrderLineDraft(
    string SkuId,
    Guid? BatchId,
    int Quantity,
    decimal BaseUnitPrice,
    decimal FinalUnitPrice,
    bool IsGift,
    string? AdjustmentsJson);

public sealed class OrderLine : BaseEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public string SkuId { get; private set; } = null!;
    public Guid? BatchId { get; private set; }
    public int Quantity { get; private set; }
    public int CancelledQty { get; private set; }
    public int ReturnedQty { get; private set; }
    public int RefundedQty { get; private set; }
    public decimal BaseUnitPrice { get; private set; }
    public decimal FinalUnitPrice { get; private set; }
    public bool IsGift { get; private set; }
    public string? AdjustmentsJson { get; private set; }

    private OrderLine() { }

    internal static OrderLine Create(Guid orderId, OrderLineDraft draft)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(draft.SkuId)) throw new ArgumentException("SkuId required.", nameof(draft));
        if (draft.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(draft.Quantity));

        return new OrderLine
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SkuId = draft.SkuId.Trim(),
            BatchId = draft.BatchId,
            Quantity = draft.Quantity,
            CancelledQty = 0,
            ReturnedQty = 0,
            RefundedQty = 0,
            BaseUnitPrice = draft.BaseUnitPrice,
            FinalUnitPrice = draft.FinalUnitPrice,
            IsGift = draft.IsGift,
            AdjustmentsJson = string.IsNullOrWhiteSpace(draft.AdjustmentsJson) ? null : draft.AdjustmentsJson.Trim()
        };
    }

    public int ActiveQty => Math.Max(0, Quantity - CancelledQty - ReturnedQty);
    public int RefundableQty => Math.Max(0, Quantity - RefundedQty);

    public void Cancel(int qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > ActiveQty) throw new InvalidOperationException("Cancel quantity exceeds available items.");
        CancelledQty += qty;
        Touch();
    }

    public void Return(int qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > ActiveQty) throw new InvalidOperationException("Return quantity exceeds available items.");
        ReturnedQty += qty;
        Touch();
    }

    public void MarkRefunded(int qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > RefundableQty) throw new InvalidOperationException("Refund quantity exceeds refundable items.");
        RefundedQty += qty;
        Touch();
    }

    public void UpdateQuantity(int newQty)
    {
        if (newQty <= 0) throw new ArgumentOutOfRangeException(nameof(newQty));
        if (newQty < CancelledQty + ReturnedQty)
            throw new InvalidOperationException("New quantity cannot be less than cancelled/returned items.");
        Quantity = newQty;
        Touch();
    }
}

public sealed class OrderTimelineEntry : BaseEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public string EventType { get; private set; } = null!;
    public OrderStatus? FromStatus { get; private set; }
    public OrderStatus? ToStatus { get; private set; }
    public string? Message { get; private set; }
    public string? DataJson { get; private set; }

    private OrderTimelineEntry() { }

    internal static OrderTimelineEntry Create(
        Guid orderId,
        string eventType,
        OrderStatus? fromStatus,
        OrderStatus? toStatus,
        string? message,
        string? dataJson,
        DateTime? whenUtc = null)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType required.", nameof(eventType));

        var ts = whenUtc ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc) ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

        return new OrderTimelineEntry
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = eventType.Trim(),
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            DataJson = string.IsNullOrWhiteSpace(dataJson) ? null : dataJson.Trim(),
            CreatedAt = ts,
            UpdatedAt = ts
        };
    }

    public static OrderTimelineEntry CreateExternal(
        Guid orderId,
        string eventType,
        OrderStatus? fromStatus,
        OrderStatus? toStatus,
        string? message,
        string? dataJson,
        DateTime? whenUtc = null) =>
        Create(orderId, eventType, fromStatus, toStatus, message, dataJson, whenUtc);
}
