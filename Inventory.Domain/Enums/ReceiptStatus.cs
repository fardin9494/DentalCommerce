namespace Inventory.Domain.Enums;

public enum ReceiptStatus
{
    Draft = 1,
    Received = 2,  // کالا وارد انبار شده اما در قرنطینه است (Waiting for Approval)
    Approved = 3,  // تایید نهایی مدیر (Available for Sale)
    Canceled = 4
}