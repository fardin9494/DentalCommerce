namespace Inventory.Domain.Enums;

public enum StockSerialStatus
{
    Draft = 0,
    Quarantine = 1,
    AwaitingShelving = 2,
    Available = 3,
    Reserved = 4,
    Issued = 5,
    Rejected = 6,
    Returned = 7,
    Disposed = 8,
    InTransit = 9
}
