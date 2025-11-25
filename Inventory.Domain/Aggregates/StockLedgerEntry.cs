using BuildingBlocks.Domain;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Aggregates;


public sealed class StockLedgerEntry : AggregateRoot<Guid>
{
    public DateTime Timestamp { get; private set; }

    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string? LotNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    public decimal DeltaQty { get; private set; }           // + برای ورود، - برای خروج
    public decimal? UnitCost { get; private set; }          // (اختیاری) برای محاسبات هزینه
    public StockMovementType MovementType { get; private set; }

    public string RefDocType { get; private set; } = null!; // مثلاً "Receipt", "Issue"
    public Guid RefDocId { get; private set; }
    public string? Note { get; private set; }

    private StockLedgerEntry() { }

   
    public static StockLedgerEntry Create(
        DateTime timestampUtc,
        Guid productId,
        Guid? variantId,
        Guid warehouseId,
        string? lotNumber,
        DateTime? expiryDate,
        decimal deltaQty,
        StockMovementType type,
        string refDocType,
        Guid refDocId,
        decimal? unitCost = null,
        string? note = null)
    {
        // گارد سازگاری نوع/علامت
        bool mustBePositive = type is StockMovementType.Receipt
            or StockMovementType.TransferIn
            or StockMovementType.AdjustmentPlus;

        bool mustBeNegative = type is StockMovementType.Issue
            or StockMovementType.TransferOut
            or StockMovementType.AdjustmentMinus;

        if (mustBePositive && deltaQty <= 0)
            throw new InvalidOperationException("deltaQty باید مثبت باشد برای این نوع حرکت.");
        if (mustBeNegative && deltaQty >= 0)
            throw new InvalidOperationException("deltaQty باید منفی باشد برای این نوع حرکت.");

        // نوع میراثی Adjustment را آزاد می‌گذاریم (قدیمی)
        // ...

        return new StockLedgerEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc),
            ProductId = productId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber,
            ExpiryDate = expiryDate,
            DeltaQty = deltaQty,
            MovementType = type,
            RefDocType = refDocType,
            RefDocId = refDocId,
            UnitCost = unitCost,
            Note = string.IsNullOrWhiteSpace(note) ? null : note
        };
    }
}
