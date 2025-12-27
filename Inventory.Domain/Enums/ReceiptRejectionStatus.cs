namespace Inventory.Domain.Enums;

public enum ReceiptRejectionStatus
{
    None = 0,
    Pending = 1,
    ApprovedToStock = 2,
    Returned = 3,
    Disposed = 4,
    Mixed = 5
}
