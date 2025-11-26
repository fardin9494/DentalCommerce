using BuildingBlocks.Domain;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Aggregates;

public sealed class Receipt : AggregateRoot<Guid>
{
    private readonly List<ReceiptLine> _lines = new();

    public Guid WarehouseId { get; private set; }
    public string? ExternalRef { get; private set; } // شماره‌ی فاکتور تامین‌کننده/ارجاع خارجی (اختیاری)
    public DateTime DocDate { get; private set; }    // تاریخ سند (UTC)
    public ReceiptStatus Status { get; private set; } = ReceiptStatus.Draft;
    public DateTime? ReceivedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    public ReceiptReason Reason { get; private set; }

    public IReadOnlyList<ReceiptLine> Lines => _lines;

    private Receipt()
    {
       
    }

    public static Receipt Create(Guid warehouseId, ReceiptReason reason, DateTime? docDateUtc = null, string? externalRef = null)
        => new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Reason = reason,
            DocDate = DateTime.SpecifyKind(docDateUtc ?? DateTime.UtcNow, DateTimeKind.Utc),
            ExternalRef = string.IsNullOrWhiteSpace(externalRef) ? null : externalRef.Trim()
        };

    public ReceiptLine AddLine(Guid productId, Guid? variantId, decimal qty, string? lotNumber, DateTime? expiryDateUtc, decimal? unitCost)
    {
        EnsureDraft();
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        var line = ReceiptLine.Create(Id, _lines.Count + 1, productId, variantId, qty, lotNumber, expiryDateUtc, unitCost);
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
        // رینامبر ساده
        for (int i = 0; i < _lines.Count; i++) _lines[i].Renumber(i + 1);
        Touch();
    }

    public void Receive(DateTime? whenUtc = null)
    {
        EnsureStatus(ReceiptStatus.Draft);
        if (_lines.Count == 0) throw new InvalidOperationException("رسید بدون آیتم قابل دریافت نیست.");

        Status = ReceiptStatus.Received;
        ReceivedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    // متد جدید: مرحله دوم - تایید توسط مدیر
    public void Approve(DateTime? whenUtc = null)
    {
        EnsureStatus(ReceiptStatus.Received); // فقط رسید دریافت شده قابل تایید است

        Status = ReceiptStatus.Approved;
        ApprovedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    // تغییر کوچک در Cancel برای سازگاری
    public void Cancel()
    {
        if (Status != ReceiptStatus.Draft) throw new InvalidOperationException("فقط رسید پیش‌نویس قابل ابطال است.");
        Status = ReceiptStatus.Canceled;
        Touch();
    }

    // Helper method
    private void EnsureStatus(ReceiptStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"عملیات در وضعیت {Status} مجاز نیست. وضعیت باید {expected} باشد.");
    }


    private void EnsureDraft()
    {
        if (Status != ReceiptStatus.Draft) throw new InvalidOperationException("در وضعیت جاری قابل تغییر نیست.");
    }
}

public sealed class ReceiptLine : BaseEntity<Guid>
{
    public Guid ReceiptId { get; private set; }
    public int LineNo { get; private set; }           // شماره خط
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal Qty { get; private set; }
    public string? LotNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; } // UTC
    public decimal? UnitCost { get; private set; }

    private ReceiptLine() { }

    internal static ReceiptLine Create(Guid receiptId, int lineNo, Guid productId, Guid? variantId, decimal qty, string? lotNumber, DateTime? expiryDateUtc, decimal? unitCost)
        => new()
        {
            Id = Guid.NewGuid(),
            ReceiptId = receiptId,
            LineNo = lineNo,
            ProductId = productId,
            VariantId = variantId,
            Qty = qty,
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(),
            ExpiryDate = expiryDateUtc is null ? null : DateTime.SpecifyKind(expiryDateUtc.Value, DateTimeKind.Utc),
            UnitCost = unitCost
        };

    internal void Renumber(int no) => LineNo = no;
}
