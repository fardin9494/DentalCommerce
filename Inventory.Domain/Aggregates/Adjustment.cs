using BuildingBlocks.Domain;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Aggregates;

public sealed class Adjustment : AggregateRoot<Guid>
{
    private readonly List<AdjustmentLine> _lines = new();

    public Guid WarehouseId { get; private set; }
    public AdjustmentStatus Status { get; private set; } = AdjustmentStatus.Draft;
    public AdjustmentReason Reason { get; private set; }
    public string? Note { get; private set; }
    public DateTime DocDate { get; private set; } // UTC
    public DateTime? PostedAt { get; private set; }

    public IReadOnlyList<AdjustmentLine> Lines => _lines;

    private Adjustment() { }

    public static Adjustment Create(Guid warehouseId, AdjustmentReason reason, DateTime? docDateUtc = null, string? note = null)
    {
        if (warehouseId == Guid.Empty) throw new ArgumentException(nameof(warehouseId));
        return new Adjustment
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Reason = reason,
            DocDate = DateTime.SpecifyKind(docDateUtc ?? DateTime.UtcNow, DateTimeKind.Utc),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    /// <summary>
    /// Add an adjustment line tied to a specific StockItem.
    /// qtyDelta: + increase / - decrease
    /// </summary>
    public AdjustmentLine AddLine(Guid stockItemId, Guid productId, Guid? variantId, string? lotNumber, DateTime? expiryDateUtc, decimal qtyDelta)
    {
        EnsureDraft();
        if (qtyDelta == 0) throw new InvalidOperationException("مقدار نمی‌تواند صفر باشد.");
        var line = AdjustmentLine.Create(Id, _lines.Count + 1, stockItemId, productId, variantId, lotNumber, expiryDateUtc, qtyDelta);
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

    public void SetNote(string? note)
    {
        EnsureDraft();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch();
    }

    public void UpdateHeader(string? note, DateTime? docDateUtc)
    {
        EnsureDraft();
        if (note != null) Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (docDateUtc.HasValue) DocDate = DateTime.SpecifyKind(docDateUtc.Value, DateTimeKind.Utc);
        Touch();
    }

    public void Post(DateTime? whenUtc = null)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new InvalidOperationException("هیچ خطی برای ثبت وجود ندارد.");
        Status = AdjustmentStatus.Posted;
        PostedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    public void Cancel()
    {
        if (Status != AdjustmentStatus.Draft) throw new InvalidOperationException("فقط پیش‌نویس‌ها قابل لغو هستند.");
        Status = AdjustmentStatus.Canceled;
        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != AdjustmentStatus.Draft) throw new InvalidOperationException("اصلاح باید در وضعیت پیش‌نویس باشد تا ویرایش شود.");
    }
}

public sealed class AdjustmentLine : BaseEntity<Guid>
{
    public Guid AdjustmentId { get; private set; }
    public int LineNo { get; private set; }
    public Guid StockItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string? LotNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; } // UTC
    public decimal QtyDelta { get; private set; }     // + increase / - decrease

    private AdjustmentLine() { }

    internal static AdjustmentLine Create(Guid adjustmentId, int lineNo, Guid stockItemId, Guid productId, Guid? variantId, string? lotNumber, DateTime? expiryDateUtc, decimal qtyDelta)
    {
        if (stockItemId == Guid.Empty) throw new ArgumentException("شناسه موجودی الزامی است.", nameof(stockItemId));
        if (productId == Guid.Empty) throw new ArgumentException("شناسه محصول الزامی است.", nameof(productId));
        if (qtyDelta == 0) throw new InvalidOperationException("مقدار نمی‌تواند صفر باشد.");

        return new()
        {
            Id = Guid.NewGuid(),
            AdjustmentId = adjustmentId,
            LineNo = lineNo,
            StockItemId = stockItemId,
            ProductId = productId,
            VariantId = variantId,
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(),
            ExpiryDate = expiryDateUtc.HasValue ? DateTime.SpecifyKind(expiryDateUtc.Value, DateTimeKind.Utc) : null,
            QtyDelta = qtyDelta
        };
    }

    internal void Renumber(int no) => LineNo = no;

    public void UpdateQtyDelta(decimal qtyDelta)
    {
        if (qtyDelta == 0) throw new InvalidOperationException("مقدار نمی‌تواند صفر باشد.");
        QtyDelta = qtyDelta;
    }
}
