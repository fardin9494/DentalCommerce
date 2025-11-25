namespace Inventory.Domain.Enums;

public enum ReceiptReason
{
    Purchase = 1,      // خرید از تامین‌کننده
    ReturnIn = 2,      // مرجوعی به انبار
    Production = 3,    // تولید/تجمیع
    Other = 99
}