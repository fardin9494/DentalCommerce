using BuildingBlocks.Domain;
using Inventory.Domain.Enums;
using Inventory.Domain.Naming;

namespace Inventory.Domain.Aggregates;

public sealed class Receipt : AggregateRoot<Guid>
{
    private readonly List<ReceiptLine> _lines = new();

    public Guid WarehouseId { get; private set; }
    public long DocNo { get; private set; }
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

    public static Receipt Create(Guid warehouseId, ReceiptReason reason, long docNo, DateTime? docDateUtc = null, string? externalRef = null)
    {
        if (docNo <= 0) throw new ArgumentOutOfRangeException(nameof(docNo));
        var rec = new Receipt
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            DocNo = docNo,
            Reason = reason,
            DocDate = DateTime.SpecifyKind(docDateUtc ?? DateTime.UtcNow, DateTimeKind.Utc),
        };

        rec.ExternalRef = NormalizeExternalRef(externalRef) ?? InventoryDocumentReference.ForReceipt(rec.Reason, rec.DocDate, rec.DocNo);
        return rec;
    }

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

    public void UpdateHeader(string? externalRef, DateTime? docDateUtc)
    {
        EnsureDraft();
        if (docDateUtc.HasValue) DocDate = DateTime.SpecifyKind(docDateUtc.Value, DateTimeKind.Utc);
        if (externalRef != null)
            ExternalRef = NormalizeExternalRef(externalRef) ?? InventoryDocumentReference.ForReceipt(Reason, DocDate, DocNo);
        Touch();
    }

    private void EnsureExternalRef()
    {
        if (DocNo <= 0) throw new InvalidOperationException("DocNo is not set for receipt.");
        ExternalRef ??= InventoryDocumentReference.ForReceipt(Reason, DocDate, DocNo);
    }

    private static string? NormalizeExternalRef(string? externalRef)
        => string.IsNullOrWhiteSpace(externalRef) ? null : externalRef.Trim();

    public void Receive(DateTime? whenUtc = null)
    {
        EnsureStatus(ReceiptStatus.Draft);
        EnsureExternalRef();
        if (_lines.Count == 0) throw new InvalidOperationException("رسید بدون آیتم قابل دریافت نیست.");

        Status = ReceiptStatus.Received;
        ReceivedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    // متد جدید: مرحله دوم - تایید توسط مدیر (تایید کلی - برای سازگاری با کد قدیمی)
    public void Approve(DateTime? whenUtc = null)
    {
        EnsureStatus(ReceiptStatus.Received); // فقط رسید دریافت شده قابل تایید است

        EnsureExternalRef();

        Status = ReceiptStatus.Approved;
        ApprovedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    /// <summary>
    /// بررسی می‌کند که آیا همه خطوط به طور کامل تایید یا رد شده‌اند
    /// این متد فقط بررسی می‌کند و وضعیت را تغییر نمی‌دهد
    /// </summary>
    public bool IsReadyForFinalApproval()
    {
        if (Status != ReceiptStatus.Received) return false;
        return _lines.All(l => l.IsFullyApproved);
    }

    /// <summary>
    /// تایید نهایی رسید - فقط وقتی همه خطوط تکلیفشان مشخص شده باشد
    /// </summary>
    public void FinalApprove(DateTime? whenUtc = null)
    {
        EnsureStatus(ReceiptStatus.Received);
        EnsureExternalRef();
        
        if (!IsReadyForFinalApproval())
            throw new InvalidOperationException("همه خطوط باید به طور کامل تایید یا رد شده باشند.");
        
        Status = ReceiptStatus.Approved;
        ApprovedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    /// <summary>
    /// پیدا کردن خط بر اساس شناسه
    /// </summary>
    public ReceiptLine? FindLine(Guid lineId) => _lines.FirstOrDefault(l => l.Id == lineId);

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
    
    // فیلدهای جدید برای تایید/رد جزئی
    public decimal ApprovedQty { get; private set; } = 0;      // مقدار تایید شده
    public decimal RejectedQty { get; private set; } = 0;      // مقدار رد شده
    public decimal RejectionApprovedQty { get; private set; } = 0;
    public decimal RejectionReturnedQty { get; private set; } = 0;
    public decimal RejectionDisposedQty { get; private set; } = 0;
    public string? RejectionReason { get; private set; }      // دلیل رد
    public ReceiptRejectionStatus RejectionStatus { get; private set; } = ReceiptRejectionStatus.None;
    public DateTime? RejectionResolvedAt { get; private set; }
    public string? RejectionResolutionNote { get; private set; }
    public decimal RejectionResolvedQty => RejectionApprovedQty + RejectionReturnedQty + RejectionDisposedQty;
    public decimal RejectionRemainingQty => RejectedQty - RejectionResolvedQty;

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

    public void UpdateQty(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        Qty = qty;
    }

    public void UpdateUnitCost(decimal? unitCost)
    {
        UnitCost = unitCost;
    }

    public void UpdateLotNumber(string? lotNumber)
    {
        LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim();
    }

    public void UpdateExpiryDate(DateTime? expiryDateUtc)
    {
        ExpiryDate = expiryDateUtc is null ? null : DateTime.SpecifyKind(expiryDateUtc.Value, DateTimeKind.Utc);
    }

    /// <summary>
    /// تایید جزئی یک خط - مقدار مشخصی را تایید می‌کند (می‌تواند افزایش یا کاهش باشد)
    /// </summary>
    public void ApprovePartial(decimal newApprovedQty)
    {
        if (newApprovedQty < 0) throw new ArgumentOutOfRangeException(nameof(newApprovedQty), "مقدار تایید نمی‌تواند منفی باشد.");
        if (newApprovedQty + RejectedQty > Qty)
            throw new InvalidOperationException($"مجموع مقادیر تایید شده و رد شده نمی‌تواند از مقدار کل خط ({Qty}) بیشتر باشد.");
        
        ApprovedQty = newApprovedQty;
    }

    /// <summary>
    /// رد کردن یک خط - مقدار مشخصی را رد می‌کند و دلیل رد را ثبت می‌کند (می‌تواند افزایش یا کاهش باشد)
    /// </summary>
        public void Reject(decimal newRejectedQty, string? reason = null)
    {
        if (newRejectedQty < 0) throw new ArgumentOutOfRangeException(nameof(newRejectedQty), "مقدار رد نمی‌تواند منفی باشد.");
        if (ApprovedQty + newRejectedQty > Qty)
            throw new InvalidOperationException($"مجموع مقادیر تایید شده و رد شده نمی‌تواند از مقدار کل خط ({Qty}) بیشتر باشد.");
        if (newRejectedQty < RejectionResolvedQty)
            throw new InvalidOperationException("مقدار رد نمی‌تواند از مقدار تعیین تکلیف شده کمتر باشد.");

        RejectedQty = newRejectedQty;

        if (newRejectedQty == 0)
        {
            RejectionReason = null;
            ClearRejectionResolution();
            return;
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            // اگر مقدار رد شده افزایش یافته، دلیل جدید را اضافه می‌کنیم
            // اگر کاهش یافته، دلیل را پاک می‌کنیم (یا می‌توانیم نگه داریم)
            if (newRejectedQty > 0)
            {
                RejectionReason = string.IsNullOrWhiteSpace(RejectionReason)
                    ? reason.Trim()
                    : $"{RejectionReason}\n{reason.Trim()}";
            }
            else
            {
                RejectionReason = null;
            }
        }

        UpdateRejectionResolutionStatus();
        Touch();
    }

    public void MarkRejectionPending()
    {
        UpdateRejectionResolutionStatus();
        Touch();
    }

    public void ResolveRejection(ReceiptRejectionStatus status, string? note = null, DateTime? whenUtc = null)
    {
        if (RejectedQty <= 0)
            throw new InvalidOperationException("برای مقدار رد صفر، وضعیت رسیدگی معنی ندارد.");
        if (status is ReceiptRejectionStatus.None or ReceiptRejectionStatus.Pending or ReceiptRejectionStatus.Mixed)
            throw new InvalidOperationException("وضعیت رسیدگی نامعتبر است.");

        var remaining = RejectionRemainingQty;
        if (remaining <= 0)
            throw new InvalidOperationException("این خط قبلا تعیین تکلیف شده است.");

        switch (status)
        {
            case ReceiptRejectionStatus.ApprovedToStock:
                ResolveRejectionAmounts(remaining, 0, 0, note, whenUtc);
                break;
            case ReceiptRejectionStatus.Returned:
                ResolveRejectionAmounts(0, remaining, 0, note, whenUtc);
                break;
            case ReceiptRejectionStatus.Disposed:
                ResolveRejectionAmounts(0, 0, remaining, note, whenUtc);
                break;
            default:
                throw new InvalidOperationException("وضعیت رسیدگی نامعتبر است.");
        }
    }

    public void ResolveRejectionAmounts(decimal approvedQty, decimal returnedQty, decimal disposedQty, string? note = null, DateTime? whenUtc = null)
    {
        if (RejectedQty <= 0)
            throw new InvalidOperationException("برای مقدار رد صفر، وضعیت رسیدگی معنی ندارد.");
        if (approvedQty < 0 || returnedQty < 0 || disposedQty < 0)
            throw new ArgumentOutOfRangeException(nameof(approvedQty), "مقادیر تعیین تکلیف نمی‌توانند منفی باشند.");

        var deltaTotal = approvedQty + returnedQty + disposedQty;
        if (deltaTotal <= 0)
            throw new InvalidOperationException("حداقل یکی از مقادیر تعیین تکلیف باید بزرگتر از صفر باشد.");

        var newApproved = RejectionApprovedQty + approvedQty;
        var newReturned = RejectionReturnedQty + returnedQty;
        var newDisposed = RejectionDisposedQty + disposedQty;
        var newResolved = newApproved + newReturned + newDisposed;

        if (newResolved > RejectedQty)
            throw new InvalidOperationException("مجموع مقادیر تعیین تکلیف نمی‌تواند از مقدار رد شده بیشتر باشد.");

        RejectionApprovedQty = newApproved;
        RejectionReturnedQty = newReturned;
        RejectionDisposedQty = newDisposed;

        if (!string.IsNullOrWhiteSpace(note))
            RejectionResolutionNote = note.Trim();

        UpdateRejectionResolutionStatus(whenUtc);
        Touch();
    }

    public void ClearRejectionResolution()
    {
        RejectionStatus = ReceiptRejectionStatus.None;
        RejectionResolvedAt = null;
        RejectionResolutionNote = null;
        RejectionApprovedQty = 0;
        RejectionReturnedQty = 0;
        RejectionDisposedQty = 0;
        Touch();
    }

    private void UpdateRejectionResolutionStatus(DateTime? whenUtc = null)
    {
        if (RejectedQty <= 0)
        {
            RejectionStatus = ReceiptRejectionStatus.None;
            RejectionResolvedAt = null;
            RejectionResolutionNote = null;
            return;
        }

        if (RejectionRemainingQty > 0)
        {
            RejectionStatus = ReceiptRejectionStatus.Pending;
            RejectionResolvedAt = null;
            if (RejectionResolvedQty <= 0)
                RejectionResolutionNote = null;
            return;
        }

        SetFinalRejectionStatus(whenUtc);
    }

    private void SetFinalRejectionStatus(DateTime? whenUtc = null)
    {
        var approved = RejectionApprovedQty > 0;
        var returned = RejectionReturnedQty > 0;
        var disposed = RejectionDisposedQty > 0;
        var kindCount = (approved ? 1 : 0) + (returned ? 1 : 0) + (disposed ? 1 : 0);

        if (kindCount <= 0)
        {
            RejectionStatus = ReceiptRejectionStatus.Pending;
            RejectionResolvedAt = null;
            return;
        }

        RejectionStatus = kindCount > 1
            ? ReceiptRejectionStatus.Mixed
            : approved ? ReceiptRejectionStatus.ApprovedToStock
            : returned ? ReceiptRejectionStatus.Returned
            : ReceiptRejectionStatus.Disposed;

        RejectionResolvedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
    }
    /// <summary>
    /// محاسبه مقدار تغییر (افزایش یا کاهش) برای تایید
    /// </summary>
    public decimal GetApproveDelta(decimal newApprovedQty) => newApprovedQty - ApprovedQty;

    /// <summary>
    /// محاسبه مقدار تغییر (افزایش یا کاهش) برای رد
    /// </summary>
    public decimal GetRejectDelta(decimal newRejectedQty) => newRejectedQty - RejectedQty;

    /// <summary>
    /// بررسی می‌کند که آیا خط به طور کامل تایید شده است یا نه
    /// </summary>
    public bool IsFullyApproved => ApprovedQty + RejectedQty >= Qty;

    /// <summary>
    /// مقدار باقیمانده برای تایید/رد
    /// </summary>
    public decimal RemainingQty => Qty - ApprovedQty - RejectedQty;
}

