using BuildingBlocks.Domain;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Aggregates;

public sealed class StockItemSerial : BaseEntity<Guid>
{
    public Guid ReceiptLineId { get; private set; }
    public Guid? StockItemId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
    public StockSerialStatus Status { get; private set; } = StockSerialStatus.Draft;
    public Guid? IssueId { get; private set; }
    public Guid? IssueLineId { get; private set; }
    public DateTime? IssuedAt { get; private set; }
    public Guid? TransferId { get; private set; }
    public Guid? TransferLineId { get; private set; }
    public Guid? TransferSegmentId { get; private set; }

    private StockItemSerial() { }

    public static StockItemSerial Create(Guid receiptLineId, string serialNumber)
    {
        if (receiptLineId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(receiptLineId));
        if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("Serial number is required.", nameof(serialNumber));

        return new StockItemSerial
        {
            Id = Guid.NewGuid(),
            ReceiptLineId = receiptLineId,
            SerialNumber = serialNumber.Trim(),
            Status = StockSerialStatus.Draft
        };
    }

    public void AttachToStock(Guid stockItemId)
    {
        if (stockItemId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(stockItemId));
        StockItemId = stockItemId;
        Status = StockSerialStatus.Quarantine;
        ClearTransferRef();
        Touch();
    }

    public void MarkAvailable(bool isShelved)
    {
        Status = isShelved ? StockSerialStatus.Available : StockSerialStatus.AwaitingShelving;
        ClearIssueRef();
        ClearTransferRef();
        Touch();
    }

    public void MarkQuarantine()
    {
        Status = StockSerialStatus.Quarantine;
        ClearIssueRef();
        ClearTransferRef();
        Touch();
    }

    public void Reserve(Guid issueId, Guid issueLineId)
    {
        if (issueId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(issueId));
        if (issueLineId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(issueLineId));
        Status = StockSerialStatus.Reserved;
        IssueId = issueId;
        IssueLineId = issueLineId;
        IssuedAt = null;
        Touch();
    }

    public void ReleaseReservation(bool isShelved)
    {
        Status = isShelved ? StockSerialStatus.Available : StockSerialStatus.AwaitingShelving;
        ClearIssueRef();
        Touch();
    }

    public void ReserveForTransfer(Guid transferId, Guid transferLineId, Guid transferSegmentId)
    {
        if (transferId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(transferId));
        if (transferLineId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(transferLineId));
        if (transferSegmentId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(transferSegmentId));
        Status = StockSerialStatus.Reserved;
        TransferId = transferId;
        TransferLineId = transferLineId;
        TransferSegmentId = transferSegmentId;
        Touch();
    }

    public void ReleaseTransferReservation(bool isShelved)
    {
        Status = isShelved ? StockSerialStatus.Available : StockSerialStatus.AwaitingShelving;
        ClearTransferRef();
        Touch();
    }

    public void MarkInTransit()
    {
        Status = StockSerialStatus.InTransit;
        Touch();
    }

    public void ReceiveTransfer(Guid stockItemId, bool isShelved)
    {
        if (stockItemId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(stockItemId));
        StockItemId = stockItemId;
        Status = isShelved ? StockSerialStatus.Available : StockSerialStatus.AwaitingShelving;
        Touch();
    }

    public void MarkIssued(Guid issueId, Guid issueLineId, DateTime whenUtc)
    {
        if (issueId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(issueId));
        if (issueLineId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(issueLineId));
        Status = StockSerialStatus.Issued;
        IssueId = issueId;
        IssueLineId = issueLineId;
        IssuedAt = DateTime.SpecifyKind(whenUtc, DateTimeKind.Utc);
        Touch();
    }

    public void MarkRejected()
    {
        Status = StockSerialStatus.Rejected;
        ClearIssueRef();
        ClearTransferRef();
        Touch();
    }

    public void MarkReturned()
    {
        Status = StockSerialStatus.Returned;
        ClearIssueRef();
        ClearTransferRef();
        Touch();
    }

    public void MarkDisposed()
    {
        Status = StockSerialStatus.Disposed;
        ClearIssueRef();
        ClearTransferRef();
        Touch();
    }

    private void ClearIssueRef()
    {
        IssueId = null;
        IssueLineId = null;
        IssuedAt = null;
    }

    private void ClearTransferRef()
    {
        TransferId = null;
        TransferLineId = null;
        TransferSegmentId = null;
    }
}
