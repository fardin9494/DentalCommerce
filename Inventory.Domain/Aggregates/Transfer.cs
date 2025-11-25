using BuildingBlocks.Domain;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Aggregates;

public sealed class Transfer : AggregateRoot<Guid>
{
    private readonly List<TransferLine> _lines = new();

    public Guid SourceWarehouseId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public string? ExternalRef { get; private set; }
    public DateTime DocDate { get; private set; }     // UTC
    public TransferStatus Status { get; private set; } = TransferStatus.Draft;
    public DateTime? ShippedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<TransferLine> Lines => _lines;

    private Transfer() { }

    public static Transfer Create(Guid sourceWhId, Guid destWhId, DateTime? docDateUtc = null, string? externalRef = null)
    {
        if (sourceWhId == destWhId) throw new InvalidOperationException("انبار مبدا و مقصد نمی‌توانند یکسان باشند.");
        return new Transfer
        {
            Id = Guid.NewGuid(),
            SourceWarehouseId = sourceWhId,
            DestinationWarehouseId = destWhId,
            DocDate = DateTime.SpecifyKind(docDateUtc ?? DateTime.UtcNow, DateTimeKind.Utc),
            ExternalRef = string.IsNullOrWhiteSpace(externalRef) ? null : externalRef.Trim()
        };
    }

    public TransferLine AddLine(Guid productId, Guid? variantId, decimal qty)
    {
        EnsureDraft();
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        var line = TransferLine.Create(Id, _lines.Count + 1, productId, variantId, qty);
        _lines.Add(line);
        Touch();
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var idx = _lines.FindIndex(l => l.Id == lineId);
        if (idx < 0) return;
        _lines.RemoveAt(idx);
        for (int i = 0; i < _lines.Count; i++) _lines[i].Renumber(i + 1);
        Touch();
    }

    public void Ship(DateTime? whenUtc = null)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new InvalidOperationException("سند انتقال بدون آیتم قابل ارسال نیست.");
        if (_lines.Any(l => l.RemainingQty > 0))
            throw new InvalidOperationException("همه‌ی خطوط باید کامل تخصیص داده شوند.");

        Status = TransferStatus.Shipped;
        ShippedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    public void AfterReceiveEvaluateCompletion()
    {
        if (Lines.SelectMany(x => x.Segments).All(s => s.RemainingToReceive <= 0))
        {
            Status = TransferStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
        else
        {
            Status = TransferStatus.PartiallyReceived;
        }
        Touch();
    }

    public void Cancel()
    {
        if (Status != TransferStatus.Draft) throw new InvalidOperationException("فقط در وضعیت پیش‌نویس قابل ابطال است.");
        Status = TransferStatus.Canceled;
        Touch();
    }

    // رَپرهای امن برای دسترسی از Handlerها

    public TransferSegment AddSegment(Guid lineId, Guid stockItemId, decimal qty)
    {
        EnsureDraft();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new InvalidOperationException("خط سند انتقال پیدا نشد.");
        var seg = line.AddSegment(stockItemId, qty);
        Touch();
        return seg;
    }

    public void ClearSegments(Guid lineId)
    {
        EnsureDraft();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new InvalidOperationException("خط سند انتقال پیدا نشد.");
        line.ClearSegments();
        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != TransferStatus.Draft) throw new InvalidOperationException("در وضعیت جاری قابل تغییر نیست.");
    }

    // inside Inventory.Domain/Aggregates/Transfer.cs
    public void ReceiveOnSegment(Guid segmentId, decimal qty)
    {
        // اجازه دریافت فقط وقتی محموله ارسال شده یا بخشی دریافت شده
        if (Status is not (TransferStatus.Shipped or TransferStatus.PartiallyReceived))
            throw new InvalidOperationException("در این وضعیت امکان ثبت دریافت نیست.");

        // پیدا کردن سگمنت در خطوط همین ترنسفر
        var seg = _lines.SelectMany(l => l.Segments).FirstOrDefault(s => s.Id == segmentId)
                  ?? throw new InvalidOperationException("Segment مربوط به این سند انتقال یافت نشد.");

        // متد داخلی را همین‌جا (در همان اسمبلی دامنه) صدا می‌زنیم
        seg.Receive(qty);

        Touch();
    }

}

public sealed class TransferLine : BaseEntity<Guid>
{
    private readonly List<TransferSegment> _segments = new();

    public Guid TransferId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal RequestedQty { get; private set; }

    public IReadOnlyList<TransferSegment> Segments => _segments;

    public decimal AllocatedQty => _segments.Sum(a => a.Qty);
    public decimal RemainingQty => RequestedQty - AllocatedQty;

    private TransferLine() { }

    internal static TransferLine Create(Guid transferId, int lineNo, Guid productId, Guid? variantId, decimal qty)
        => new()
        {
            Id = Guid.NewGuid(),
            TransferId = transferId,
            LineNo = lineNo,
            ProductId = productId,
            VariantId = variantId,
            RequestedQty = qty
        };

    internal void Renumber(int no) => LineNo = no;

    internal TransferSegment AddSegment(Guid stockItemId, decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > RemainingQty) throw new InvalidOperationException("بیش از مقدار موردنیاز تخصیص داده شده است.");
        var s = TransferSegment.Create(Id, stockItemId, qty);
        _segments.Add(s);
        return s;
    }

    internal void ClearSegments() => _segments.Clear();
}

public sealed class TransferSegment : BaseEntity<Guid>
{
    public Guid TransferLineId { get; private set; }
    public Guid StockItemId { get; private set; }  // از مبدا
    public decimal Qty { get; private set; }       // مقدار ارسال‌شونده
    public decimal ReceivedQty { get; private set; } // مقدار دریافت‌شده در مقصد

    public decimal RemainingToReceive => Qty - ReceivedQty;

    private TransferSegment() { }

    internal static TransferSegment Create(Guid lineId, Guid stockItemId, decimal qty)
        => new()
        {
            Id = Guid.NewGuid(),
            TransferLineId = lineId,
            StockItemId = stockItemId,
            Qty = qty,
            ReceivedQty = 0
        };

    internal void Receive(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (ReceivedQty + qty > Qty) throw new InvalidOperationException("بیش از مقدار مجاز دریافت می‌شود.");
        ReceivedQty += qty;
    }


}
