using BuildingBlocks.Domain;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Aggregates;

public sealed class Issue : AggregateRoot<Guid>
{
    private readonly List<IssueLine> _lines = new();

    public Guid WarehouseId { get; private set; }
    public string? ExternalRef { get; private set; }
    public DateTime DocDate { get; private set; }       // UTC
    public IssueStatus Status { get; private set; } = IssueStatus.Draft;
    public DateTime? PostedAt { get; private set; }

    public IReadOnlyList<IssueLine> Lines => _lines;

    private Issue() { }

    public static Issue Create(Guid warehouseId, DateTime? docDateUtc = null, string? externalRef = null)
        => new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            DocDate = DateTime.SpecifyKind(docDateUtc ?? DateTime.UtcNow, DateTimeKind.Utc),
            ExternalRef = string.IsNullOrWhiteSpace(externalRef) ? null : externalRef.Trim()
        };

    public IssueLine AddLine(Guid productId, Guid? variantId, decimal qty)
    {
        EnsureDraft();
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        var line = IssueLine.Create(Id, _lines.Count + 1, productId, variantId, qty);
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

    public void UpdateHeader(string? externalRef, DateTime? docDateUtc)
    {
        EnsureDraft();
        if (externalRef != null) ExternalRef = string.IsNullOrWhiteSpace(externalRef) ? null : externalRef.Trim();
        if (docDateUtc.HasValue) DocDate = DateTime.SpecifyKind(docDateUtc.Value, DateTimeKind.Utc);
        Touch();
    }

    public void Post(DateTime? whenUtc = null)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new InvalidOperationException("سند خروج بدون آیتم قابل پست نیست.");
        if (_lines.Any(l => l.RemainingQty > 0))
            throw new InvalidOperationException("همه‌ی خطوط باید کامل تخصیص داده شوند (RemainingQty=0).");

        Status = IssueStatus.Posted;
        PostedAt = DateTime.SpecifyKind(whenUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        Touch();
    }

    public void Cancel()
    {
        if (Status != IssueStatus.Draft) throw new InvalidOperationException("فقط سند پیش‌نویس قابل ابطال است.");
        Status = IssueStatus.Canceled;
        Touch();
    }

    public IssueAllocation AddAllocation(Guid lineId, Guid stockItemId, decimal qty)
    {
        EnsureDraft();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new InvalidOperationException("خط سند خروج پیدا نشد.");
        var alloc = line.AddAllocation(stockItemId, qty);
        Touch();
        return alloc;
    }

    public void ClearAllocations(Guid lineId)
    {
        EnsureDraft();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new InvalidOperationException("خط سند خروج پیدا نشد.");
        line.ClearAllocations();
        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != IssueStatus.Draft) throw new InvalidOperationException("در وضعیت جاری قابل تغییر نیست.");
    }
}

public sealed class IssueLine : BaseEntity<Guid>
{
    private readonly List<IssueAllocation> _allocations = new();

    public Guid IssueId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal RequestedQty { get; private set; }

    public IReadOnlyList<IssueAllocation> Allocations => _allocations;

    public decimal AllocatedQty => _allocations.Sum(a => a.Qty);
    public decimal RemainingQty => RequestedQty - AllocatedQty;

    private IssueLine() { }

    internal static IssueLine Create(Guid issueId, int lineNo, Guid productId, Guid? variantId, decimal qty)
        => new()
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            LineNo = lineNo,
            ProductId = productId,
            VariantId = variantId,
            RequestedQty = qty
        };

    internal void Renumber(int no) => LineNo = no;

    public void UpdateQty(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (AllocatedQty > qty) throw new InvalidOperationException("مقدار جدید نمی‌تواند کمتر از مقدار تخصیص داده شده باشد.");
        RequestedQty = qty;
    }

    internal IssueAllocation AddAllocation(Guid stockItemId, decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > RemainingQty) throw new InvalidOperationException("بیش از مقدار موردنیاز تخصیص داده شده است.");
        var a = IssueAllocation.Create(Id, stockItemId, qty);
        _allocations.Add(a);
        return a;
    }



    internal void ClearAllocations()
    {
        _allocations.Clear();
    }
}

public sealed class IssueAllocation : BaseEntity<Guid>
{
    public Guid IssueLineId { get; private set; }
    public Guid StockItemId { get; private set; }
    public decimal Qty { get; private set; }

    private IssueAllocation() { }

    internal static IssueAllocation Create(Guid lineId, Guid stockItemId, decimal qty)
        => new()
        {
            Id = Guid.NewGuid(),
            IssueLineId = lineId,
            StockItemId = stockItemId,
            Qty = qty
        };
}
