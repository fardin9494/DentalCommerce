namespace Inventory.Domain.Enums;
public enum TransferStatus
{
    Draft = 1,     // ایجاد و افزودن خطوط
    Shipped = 2,   // ارسال از مبدا انجام شده (در حال انتقال)
    PartiallyReceived = 3, // بخشی از محموله در مقصد ثبت شده
    Completed = 4, // کل محموله دریافت شد
    Canceled = 5
}