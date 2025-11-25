namespace Inventory.Domain.Enums;

public enum StockMovementType
{
    Receipt = 1, Issue = 2, TransferOut = 3, TransferIn = 4, AdjustmentPlus = 5,
    AdjustmentMinus = 6
}